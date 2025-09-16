using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Transactions;
using LibUsbNative;
using LibUsbNative.SafeHandles;
using LibUsbSharp.Descriptor;
using LibUsbSharp.Internal;
using LibUsbSharp.Internal.Hotplug;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibUsbSharp;

public sealed class LibUsb : ILibUsb
{
    private static int _instances;
    private static ILogger<LibUsb>? _staticLogger;

    private readonly object _lock = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<LibUsb> _logger;
    private readonly ConcurrentDictionary<string, UsbDevice> _openDevices = new();
    private ILibUsbNative _libUsbNative;
    private ISafeContext? _context;
    private LibUsbEventLoop? _eventLoop;
    private int _hotplugCallbackHandle;
    private bool _disposed;

    /// <summary>
    /// Get the LibUsb library version.
    /// </summary>
    public Version GetVersion()
    {
        var version = _libUsbNative.GetVersion();
        return new Version(version.Major, version.Minor, version.Micro, version.Nano);
    }

    /// <summary>
    /// Creates the LibUsb wrapper.
    /// NOTE: Call Initialize() before enumerating or opening devices.
    /// </summary>
    /// <param name="loggerFactory">
    /// Logger factory for libusb logging. If null, logging is disabled.
    /// </param>
    public LibUsb(ILoggerFactory? loggerFactory)
    {
        if (Interlocked.CompareExchange(ref _instances, 1, 0) != 0)
        {
            throw new InvalidOperationException("Only one LibUsb instance allowed.");
        }
        try
        {
            _loggerFactory = loggerFactory ?? new NullLoggerFactory();
            _logger = _loggerFactory.CreateLogger<LibUsb>();
            _staticLogger = _logger;
            _libUsbNative = LibUsbNative.ILibUsbNative.Init();
        }
        catch (Exception)
        {
            _ = Interlocked.Exchange(ref _instances, 0);
            throw;
        }
    }

    /// <inheritdoc />
    public void Initialize(LogLevel logLevel = LogLevel.Warning)
    {
        lock (_lock)
        {
            CheckDisposed();
            if (_context is not null)
            {
                throw new InvalidOperationException($"{nameof(LibUsb)} already initialized.");
            }

            _context = _libUsbNative.CreateContext();
            _logger.LogInformation("LibUsb v{LibUsbVersion} initialized.", GetVersion());

            InitializeLogging(_context!, logLevel);
            _eventLoop = new LibUsbEventLoop(_loggerFactory, _context!);
            _eventLoop.Start();
        }
    }

    private void InitializeLogging(ISafeContext context, LogLevel logLevel)
    {
        if (logLevel == LogLevel.None)
        {
            return;
        }

        try
        {
            context.RegisterLogCallback((level, message) => LibUsbLogHandler((LibUsbLogLevel)level, message));
        }
        catch (LibUsbNative.LibUsbException ex)
        {
            _logger.LogWarning(
                "Failed to set LibUsbOption.LogCallback. {ErrorMessage}",
                ((LibUsbResult)ex.Error).GetMessage()
            );
            return; // Only attempt to set log level if callback registration succeeded
        }

        var libUsbLogLevel = logLevel.ToLibUsbLogLevel();
        try
        {
            context.SetOption((int)LibUsbOption.LogLevel, (int)libUsbLogLevel);
        }
        catch (LibUsbNative.LibUsbException ex)
        {
            _logger.LogWarning(
                "Failed to set LibUsbOption.LogLevel. {ErrorMessage}",
                ((LibUsbResult)ex.Error).GetMessage()
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
        var supported = _libUsbNative.HasCapability((uint)LibUsbCapability.HasHotplug);
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
            _hotplugCallbackHandle = (int)
                GetInitializedContextOrThrow()
                    .HotplugRegisterCallback(
                        (int)(LibUsbHotplugEvent.DeviceArrived | LibUsbHotplugEvent.DeviceLeft),
                        // Set flag LibUsbHotplugFlag.Enumerate to immediately invoke the
                        // HotplugEventCallback method for currently attached devices on register
                        (int)LibUsbHotplugFlag.Enumerate,
                        vendorId is null ? HotPlugMatchAny : (int)vendorId,
                        productId is null ? HotPlugMatchAny : (int)productId,
                        deviceClass is null ? HotPlugMatchAny : (int)deviceClass,
                        IntPtr.Zero,
                        (context, device, type, data) =>
                        {
                            return HotplugEventCallback(context, device, (LibUsbHotplugEvent)type, data)
                                == LibUsbHotplugReturn.Rearm;
                        }
                    );
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
        ISafeContext context,
        ISafeDevice device,
        LibUsbHotplugEvent type,
#pragma warning disable IDE0060 // Remove unused parameter
        IntPtr userData
#pragma warning restore IDE0060 // Remove unused parameter
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(device);

        LibUsbHotplugEvent eventType = (LibUsbHotplugEvent)type;
        // TODO: Test on macOS and linux; "most functions that take a device handle are not safe"
        var result = LibUsbDeviceEnum.TryGetDeviceDescriptor(device, out var deviceDescriptor);
        if (result != LibUsbResult.Success)
        {
            _logger.LogWarning("Failed to get device descriptor. {ErrorMessage}", result.GetMessage());
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
            return LibUsbDeviceEnum.GetDeviceList(_logger, GetInitializedContextOrThrow(), vendorId, productIds);
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
            var context = GetInitializedContextOrThrow();
            using var device = OpenDeviceUnlocked(context, deviceKey);
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
            var context = GetInitializedContextOrThrow();
            return OpenDeviceUnlocked(context, deviceKey);
        }
    }

    private UsbDevice OpenDeviceUnlocked(ISafeContext context, string deviceKey)
    {
        var (deviceList, _) = context.GetDeviceList();
        try
        {
            var device = OpenListDeviceUnlocked(context, deviceList, deviceKey);
            if (!_openDevices.TryAdd(deviceKey, device))
            {
                device.Dispose();
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
            deviceList.Dispose();
        }
    }

    private UsbDevice OpenListDeviceUnlocked(ISafeContext context, ISafeDeviceList deviceList, string deviceKey)
    {
        var (device, descriptor) = LibUsbDeviceEnum
            .GetDeviceDescriptors(_logger, deviceList.Devices.ToList())
            .FirstOrDefault(d => d.Descriptor.DeviceKey == deviceKey);
        if (device is null)
        {
            throw LibUsbException.FromResult(LibUsbResult.NotFound, "Failed to get device from list.");
        }
        var configResult = LibUsbDeviceEnum.TryGetConfigDescriptor(device, out var configDescriptor);
        if (configResult != 0)
        {
            throw LibUsbException.FromResult(
                configResult,
                $"Failed to get active config descriptor for '{deviceKey}'."
            );
        }

        try
        {
            var deviceHandle = device.Open();
            return new UsbDevice(_loggerFactory, this, context, deviceHandle, descriptor, configDescriptor!);
        }
        catch (LibUsbNative.LibUsbException ex)
        {
            throw new LibUsbException(ex.Message, (LibUsbResult)ex.Error);
        }
    }

    /// <summary>
    /// Close a USB device. NOTE: Only used internally, called from UsbDevice.Dispose().
    /// </summary>
    internal void CloseDevice(string key, ISafeDeviceHandle handle)
    {
        lock (_lock)
        {
            CheckDisposed();
            _ = GetInitializedContextOrThrow();
            if (!_openDevices.TryRemove(key, out _))
            {
                throw new InvalidOperationException($"Device not found in the list of open devices.");
            }
            handle.Dispose();
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
    /// Throw InvalidOperationException when LibUsb is not initialized.
    /// </summary>
    private ISafeContext GetInitializedContextOrThrow()
    {
        return _context is null ? throw new InvalidOperationException($"No context.") : _context;
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
            if (_context is not null)
            {
                // Disabling hotplug here makes most sense, although done differently in sample code.
                // To ensure event loop exit, libusb_interrupt_event_handler is called on dispose.
                // See: https://libusb.sourceforge.io/api-1.0/group__libusb__asyncio.html#eventthread
                if (_hotplugCallbackHandle != 0)
                {
                    // NOTE: Callbacks for a context are automatically deregistered by libusb_exit()
                    _context.HotplugDeregisterCallback((IntPtr)_hotplugCallbackHandle);
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

                _context.Dispose();
                _context = null;
            }
            _staticLogger = null;
            _logger.LogDebug("LibUsb disposed.");
            _ = Interlocked.Exchange(ref _instances, 0);
            _disposed = true;
        }
    }

    private static void LibUsbLogHandler(LibUsbLogLevel level, string message)
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
                    "Unexpected libusb log level {LibUsbLogLevel}. {LibUsbMessage}",
                    level,
                    message.TrimEnd()
                );
                break;
        }
    }
}
