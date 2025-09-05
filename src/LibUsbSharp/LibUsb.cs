using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using LibUsbSharp.Descriptor;
using LibUsbSharp.Internal;
using LibUsbSharp.Internal.Hotplug;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibUsbSharp;

public sealed class LibUsb : ILibUsb
{
    internal const string LibraryName = "libusb-1.0";
    private static int _instances;
    private static ILogger<LibUsb>? _staticLogger;

    private readonly object _lock = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<LibUsb> _logger;
    private readonly ConcurrentDictionary<string, UsbDevice> _openDevices = new();
    private IntPtr _context;
    private LibUsbEventLoop? _eventLoop;
    private int _hotplugCallbackHandle;
    private bool _disposed;

    /// <summary>
    /// Get the LibUsb library version.
    /// </summary>
    public static Version GetVersion()
    {
        var versionPtr = libusb_get_version();
        var version = Marshal.PtrToStructure<LibUsbVersion>(versionPtr);
        return new Version(version.Major, version.Minor, version.Micro, version.Nano);
    }

    /// <summary>
    /// Creates the LibUsb wrapper.
    /// NOTE: Call Initialize() before enumerating or opening devices.
    /// </summary>
    /// <param name="loggerFactory">
    /// Logger factory for libusb logging. If null, logging is disabled.
    /// </param>
    /// <param name="context"></param>
    internal LibUsb(ILoggerFactory? loggerFactory, IntPtr context)
    {
        _loggerFactory = loggerFactory ?? new NullLoggerFactory();
        _logger = _loggerFactory.CreateLogger<LibUsb>();
        _staticLogger = _logger;
        _context = context;
        _eventLoop = new LibUsbEventLoop(_loggerFactory, context);
        _eventLoop.Start();
    }

    /// <summary>
    /// Initialized the LibUsb library, attaches log callback and starts the
    /// background thread that handles LibUsb events and drives async transfers.
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="logLevel">The desired LibUsb library log level.</param>
    public static LibUsb Initialize(ILoggerFactory? loggerFactory, LogLevel logLevel = LogLevel.Warning)
    {
        if (Interlocked.CompareExchange(ref _instances, 1, 0) != 0)
        {
            throw new InvalidOperationException("Only one LibUsb instance allowed.");
        }

        loggerFactory ??= new NullLoggerFactory();
        var logger = loggerFactory.CreateLogger<LibUsb>();

        var result = libusb_init(out var context);
        if (result != 0 || context == IntPtr.Zero)
        {
            throw LibUsbException.FromError(result, $"Failed to initialize {LibraryName}.");
        }

        try
        {
            InitializeLogging(logger, context, logLevel);
            logger.LogInformation("LibUsb v{LibUsbVersion} initialized.", GetVersion());

            var libusb = new LibUsb(loggerFactory, context);
            return libusb;
        }
        catch
        {
            libusb_exit(context);
            _ = Interlocked.Exchange(ref _instances, 0);
            throw;
        }
    }

    private static void InitializeLogging(ILogger logger, IntPtr context, LogLevel logLevel)
    {
        if (logLevel == LogLevel.None)
        {
            return;
        }
        var callbackResult = libusb_set_option(context, LibUsbOption.LogCallback, LibUsbLogHandler);

        if (callbackResult != 0)
        {
            logger.LogWarning(
                "Failed to set LibUsbOption.LogCallback. {ErrorMessage}",
                ((LibUsbResult)callbackResult).GetMessage()
            );
            return; // Only attempt to set log level if callback registration succeeded
        }
        var libUsbLogLevel = logLevel.ToLibUsbLogLevel();
        var levelResult = libusb_set_option(context, LibUsbOption.LogLevel, (nint)libUsbLogLevel);
        if (levelResult != 0)
        {
            logger.LogWarning(
                "Failed to set LibUsbOption.LogLevel. {ErrorMessage}",
                ((LibUsbResult)levelResult).GetMessage()
            );
        }
    }

    /// <inheritdoc />
    public bool RegisterHotplug(
        UsbClass? deviceClass = default,
        ushort? vendorId = default,
        ushort? productId = default
    )
    {
        const int HotPlugMatchAny = -1;
        var supported = libusb_has_capability((uint)LibUsbCapability.HasHotplug) != 0;
        if (!supported)
        {
            _logger.LogDebug("Hotplug not supported or unimplemented on this platform.");
            return false;
        }
        lock (_lock)
        {
            CheckDisposed();
            // We do not follow the recommended libusb init pattern: hotplug first then event loop.
            // See: https://libusb.sourceforge.io/api-1.0/group__libusb__asyncio.html#eventthread
            // This should not have any adverse effects as long as we register callback with the
            // LibUsbHotplugFlag.Enumerate flag, as it will allow catching up with current devices.
            var result = libusb_hotplug_register_callback(
                _context,
                LibUsbHotplugEvent.DeviceArrived | LibUsbHotplugEvent.DeviceLeft,
                // Set flag LibUsbHotplugFlag.Enumerate to immediately invoke the
                // HotplugEventCallback method for currently attached devices on register
                LibUsbHotplugFlag.Enumerate,
                vendorId is null ? HotPlugMatchAny : (int)vendorId,
                productId is null ? HotPlugMatchAny : (int)productId,
                deviceClass is null ? HotPlugMatchAny : (int)deviceClass,
                HotplugEventCallback,
                IntPtr.Zero,
                out var callbackHandle
            );
            if (result != 0)
            {
                throw LibUsbException.FromError(result, "Failed to register hotplug callback.");
            }
            _hotplugCallbackHandle = callbackHandle;
        }
        return true;
    }

    /// <summary>
    /// NOTE:
    /// This callback will run on the LibUsbEventLoop thread. When handling a DeviceArrived event
    /// it's considered safe to call any libusb function that takes a libusb_device. It also safe
    /// to open a device and submit asynchronous transfers. However, most other functions that take
    /// a libusb_device_handle are not safe to call. Examples of such functions are any of the
    /// synchronous API functions or the blocking functions that retrieve various USB descriptors.
    /// See: https://libusb.sourceforge.io/api-1.0/group__libusb__desc.html
    /// These functions must be used outside of the context of the hotplug callback.
    /// When handling a DeviceLeft event the only safe function is libusb_get_device_descriptor().
    /// </summary>
    private LibUsbHotplugReturn HotplugEventCallback(
        nint context,
        nint device,
        LibUsbHotplugEvent eventType,
        nint userData
    )
    {
        // TODO: Test on macOS and linux; "most functions that take a device handle are not safe"
        var result = LibUsbDeviceEnum.TryGetDeviceDescriptor(device, out var deviceDescriptor);
        if (result != LibUsbResult.Success)
        {
            _logger.LogWarning(
                "{LibUsb} get device descriptor failed. {ErrorMessage}",
                LibraryName,
                result.GetMessage()
            );
            return LibUsbHotplugReturn.Rearm;
        }
        var descriptor = deviceDescriptor!.Value;
        _logger.LogInformation(
            "Hotplug '{EventType}'. Class: {DeviceClass}. Key: {DeviceKey}.",
            eventType,
            descriptor.DeviceClass,
            descriptor.DeviceKey
        );
        return LibUsbHotplugReturn.Rearm;
    }

    /// <inheritdoc />
    public List<IUsbDeviceDescriptor> GetDeviceList(ushort? vendorId = default, HashSet<ushort>? productIds = default)
    {
        lock (_lock)
        {
            CheckDisposed();
            return LibUsbDeviceEnum.GetDeviceList(_logger, _context, vendorId, productIds);
        }
    }

    /// <inheritdoc />
    public string GetDeviceSerial(string deviceKey)
    {
        lock (_lock)
        {
            CheckDisposed();
            if (_openDevices.TryGetValue(deviceKey, out var openDevice))
            {
                return openDevice.GetSerialNumber();
            }
            using var device = OpenDeviceUnlocked(_context, deviceKey);
            return device.GetSerialNumber();
        }
    }

    /// <inheritdoc />
    public IUsbDevice OpenDevice(string deviceKey)
    {
        lock (_lock)
        {
            CheckDisposed();
            if (_openDevices.ContainsKey(deviceKey))
            {
                throw new InvalidOperationException($"Device '{deviceKey}' already open.");
            }
            return OpenDeviceUnlocked(_context, deviceKey);
        }
    }

    private UsbDevice OpenDeviceUnlocked(nint context, string deviceKey)
    {
        var listResult = libusb_get_device_list(context, out var listPtr);
        if (listResult < 0)
        {
            throw LibUsbException.FromError(listResult, "Failed to get device list.");
        }
        try
        {
            var device = OpenListDeviceUnlocked(context, listPtr, deviceKey);
            if (!_openDevices.TryAdd(deviceKey, device))
            {
                libusb_close(device.Handle);
                throw LibUsbException.FromResult(
                    LibUsbResult.OtherError,
                    $"Device with key '{deviceKey}' is already open."
                );
            }
            _logger.LogInformation("LibUsbDevice '{DeviceKey}' open.", deviceKey);
            return device;
        }
        finally
        {
            libusb_free_device_list(listPtr, 1);
        }
    }

    private UsbDevice OpenListDeviceUnlocked(nint context, nint listPtr, string deviceKey)
    {
        var (descriptorPtr, descriptor) = LibUsbDeviceEnum
            .GetDeviceDescriptors(_logger, listPtr)
            .FirstOrDefault(d => d.Descriptor.DeviceKey == deviceKey);
        if (descriptorPtr == IntPtr.Zero)
        {
            throw LibUsbException.FromResult(LibUsbResult.NotFound, "Failed to get device from list.");
        }
        var configResult = LibUsbDeviceEnum.TryGetConfigDescriptor(descriptorPtr, out var configDescriptor);
        if (configResult != 0)
        {
            throw LibUsbException.FromResult(
                configResult,
                $"Failed to get active config descriptor for '{deviceKey}'."
            );
        }
        var openResult = libusb_open(descriptorPtr, out var deviceHandle);
        return openResult == 0
            ? new UsbDevice(_loggerFactory, this, context, deviceHandle, descriptor, configDescriptor!)
            : throw LibUsbException.FromError(openResult, $"Failed to open device '{deviceKey}'.");
    }

    /// <summary>
    /// Close a USB device. NOTE: Only used internally, called from UsbDevice.Dispose().
    /// </summary>
    internal void CloseDevice(string key, nint handle)
    {
        lock (_lock)
        {
            CheckDisposed();
            if (!_openDevices.TryRemove(key, out _))
            {
                throw new InvalidOperationException($"Device not found in the list of open devices.");
            }
            libusb_close(handle);
        }
    }

    /// <summary>
    /// Throw ObjectDisposedException when LibUsb is disposed.
    /// </summary>
    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LibUsb));
        }
    }

    /// <summary>
    /// Disposes this LibUsb context and closes associated devices that remain open. Ongoing
    /// transfers are canceled, any claimed interfaces are released and allocated memory is freed.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }
            // Disabling hotplug here makes most sense, although done differently in sample code.
            // To ensure event loop exit, libusb_interrupt_event_handler is called on dispose.
            // See: https://libusb.sourceforge.io/api-1.0/group__libusb__asyncio.html#eventthread
            if (_hotplugCallbackHandle != 0)
            {
                // NOTE: Callbacks for a context are automatically deregistered by libusb_exit()
                libusb_hotplug_deregister_callback(_context, _hotplugCallbackHandle);
            }
            _eventLoop?.Dispose();
            // Close any devices, interfaces and transfers that remain open or are ongoing
            foreach (var device in _openDevices)
            {
                _logger.LogDebug("Auto disposing device '{DeviceKey}' on LibUsb dispose.", device.Key);
                // Device dispose calls LibUsb.CloseDevice, which removes it from the
                // _openDevices dictionary. This works without deadlock or race conditions since
                // the C# Monitor lock is re-entrant and the ConcurrentDictionary is designed to
                // allow modification during iteration.
                device.Value.Dispose();
            }

            // If "libusb: warning [libusb_exit] device 1.0 still referenced" or similar appears
            // in logs on libusb_exit, see https://github.com/libusb/libusb/issues/988 for info.
            libusb_exit(_context);

            _staticLogger = null;
            _logger.LogDebug("LibUsb disposed.");
            _ = Interlocked.Exchange(ref _instances, 0);
            _disposed = true;
        }
    }

    private static void LibUsbLogHandler(nint _, LibUsbLogLevel level, string message)
    {
        switch (level)
        {
            case LibUsbLogLevel.Error:
                _staticLogger?.LogError("{LibUsbMessage}", message.TrimEnd());
                break;
            case LibUsbLogLevel.Warning:
                _staticLogger?.LogWarning("{LibUsbMessage}", message.TrimEnd());
                break;
            case LibUsbLogLevel.Info:
                _staticLogger?.LogInformation("{LibUsbMessage}", message.TrimEnd());
                break;
            // LibUsbLogLevel.Debug is very verbose and is best mapped to .NET LogLevel.Trace
            case LibUsbLogLevel.Debug:
                _staticLogger?.LogTrace("{LibUsbMessage}", message.TrimEnd());
                break;
            case LibUsbLogLevel.None:
                break;
            // Catch the unlikely case that libusb adds another log level in a future version
            default:
                _staticLogger?.LogError(
                    "Unexpected {LibraryName} log level {LibUsbLogLevel}. {LibUsbMessage}",
                    LibraryName,
                    level,
                    message.TrimEnd()
                );
                break;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LibUsbLogCallback(IntPtr context, LibUsbLogLevel level, string message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate LibUsbHotplugReturn LibUsbHotplugCallback(
        IntPtr context,
        IntPtr device,
        LibUsbHotplugEvent eventType,
        IntPtr user_data
    );

    // LibraryImportAttribute not available in .NET6, silence warning
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr libusb_get_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_init(out IntPtr context);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_exit(IntPtr context);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_set_option(IntPtr context, LibUsbOption option, IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_set_option(
        IntPtr context,
        LibUsbOption option,
        LibUsbLogCallback callbackFunction
    );

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_get_device_list(IntPtr context, out IntPtr listPtr);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_free_device_list(IntPtr listPtr, int unrefDevices);

    /// <summary>
    /// Open a device and obtain a device handle. Allows you to perform I/O on the device.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_open(IntPtr deviceDescriptorPtr, out IntPtr deviceHandle);

    /// <summary>
    /// Close a device handle. Should be called on all open handles before your application exits.
    /// Internally, this function destroys the reference that was added by libusb_open().
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_close(IntPtr deviceHandle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_has_capability(uint capability);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_hotplug_register_callback(
        IntPtr context,
        LibUsbHotplugEvent events,
        LibUsbHotplugFlag flags,
        int vendorId,
        int productId,
        int deviceClass,
        LibUsbHotplugCallback callbackFunction,
        IntPtr user_data,
        out int callbackHandle
    );

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_hotplug_deregister_callback(IntPtr context, int callbackHandle);

#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'
}
