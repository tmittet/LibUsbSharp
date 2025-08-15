using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using LibUsbSharp.Descriptor;
using Microsoft.Extensions.Logging;

namespace LibUsbSharp;

public sealed class UsbDevice : IUsbDevice
{
    private readonly LibUsb _libUsb;
    private readonly nint _context;
    private readonly UsbDeviceDescriptor _descriptor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<UsbDevice> _logger;
    private readonly ConcurrentDictionary<byte, UsbInterface> _claimedInterfaces = new();
    private readonly ConcurrentDictionary<byte, string> _descriptorCache = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly object _cacheLock = new();
    private bool _disposed;

    internal nint Handle { get; init; }

    /// <inheritdoc />
    public IUsbDeviceDescriptor Descriptor => _descriptor;

    /// <inheritdoc />
    public IUsbConfigDescriptor ConfigDescriptor { get; init; }

    internal UsbDevice(
        ILoggerFactory loggerFactory,
        LibUsb libUsb,
        nint context,
        IntPtr handle,
        UsbDeviceDescriptor descriptor,
        IUsbConfigDescriptor configDescriptor
    )
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<UsbDevice>();
        _libUsb = libUsb;
        _context = context;
        Handle = handle;
        _descriptor = descriptor;
        ConfigDescriptor = configDescriptor;
    }

    /// <inheritdoc />
    public string GetManufacturer() => ReadStringDescriptorCached(_descriptor.ManufacturerIndex);

    /// <inheritdoc />
    public string GetProduct() => ReadStringDescriptorCached(_descriptor.ProductIndex);

    /// <inheritdoc />
    public string GetSerialNumber() => ReadStringDescriptorCached(_descriptor.SerialNumberIndex);

    private string ReadStringDescriptorCached(byte descriptorIndex)
    {
        if (_descriptorCache.TryGetValue(descriptorIndex, out var cachedValue1))
        {
            return cachedValue1;
        }
        lock (_cacheLock)
        {
            if (_descriptorCache.TryGetValue(descriptorIndex, out var cachedValue2))
            {
                return cachedValue2;
            }
            var value = ReadStringDescriptor(descriptorIndex);
            if (!string.IsNullOrWhiteSpace(value))
            {
                _descriptorCache[descriptorIndex] = value;
            }
            return value;
        }
    }

    /// <inheritdoc />
    public string ReadStringDescriptor(byte descriptorIndex)
    {
        _lock.EnterReadLock();
        try
        {
            CheckDisposed();
            var buffer = new byte[256];
            var result = libusb_get_string_descriptor_ascii(
                Handle,
                descriptorIndex,
                buffer,
                buffer.Length
            );
            return result >= 0
                ? Encoding.ASCII.GetString(buffer, 0, result)
                : throw LibUsbException.FromError(result, "Failed to read device serial.");
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public IUsbInterface ClaimInterface(IUsbInterfaceDescriptor descriptor)
    {
        _lock.EnterWriteLock();
        try
        {
            CheckDisposed();
            if (_claimedInterfaces.TryGetValue(descriptor.InterfaceNumber, out var existing))
            {
                throw new ArgumentException($"USB interface {existing} already claimed.");
            }
            // TODO: libusb_set_auto_detach_kernel_driver on Linux?
            var claimResult = libusb_claim_interface(Handle, descriptor.InterfaceNumber);
            if (claimResult != 0)
            {
                throw LibUsbException.FromError(
                    claimResult,
                    $"Failed to claim USB interface {descriptor}."
                );
            }
            var usbInterface = new UsbInterface(_loggerFactory, Handle, descriptor);
            // No need to check if already added, checked in TryGetValue above
            _claimedInterfaces[descriptor.InterfaceNumber] = usbInterface;
            _logger.LogDebug("USB interface {UsbInterface} claimed.", usbInterface);
            return usbInterface;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public void ReleaseInterface(byte interfaceNumber)
    {
        _lock.EnterWriteLock();
        try
        {
            CheckDisposed();
            if (!_claimedInterfaces.TryGetValue(interfaceNumber, out var usbInterface))
            {
                throw new ArgumentException(
                    $"USB interface #{interfaceNumber} not found in list of claimed interfaces."
                );
            }
            usbInterface.Dispose();
            var releaseResult = libusb_release_interface(Handle, interfaceNumber);
            if (releaseResult != 0)
            {
                throw LibUsbException.FromError(
                    releaseResult,
                    $"Failed to release USB interface {usbInterface}."
                );
            }
            if (_claimedInterfaces.TryRemove(interfaceNumber, out var _))
            {
                _logger.LogDebug("USB interface {UsbInterface} released.", usbInterface);
            }
            else
            {
                _logger.LogError(
                    "Failed to remove released USB interface {UsbInterface} from list of claimed interfaces.",
                    usbInterface
                );
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        _lock.EnterWriteLock();
        try
        {
            CheckDisposed();
            var resetResult = libusb_reset_device(Handle);
            if (resetResult != 0)
            {
                throw LibUsbException.FromError(resetResult, $"Failed to reset USB device port.");
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override string ToString() => _descriptor.DeviceKey;

    /// <summary>
    /// Throw ObjectDisposedException when UsbDevice is disposed.
    /// </summary>
    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UsbDevice));
        }
    }

    /// <summary>
    /// Disposes this device and associated resources. Ongoing transfers are canceled,
    /// claimed interfaces are automatically released and allocated memory is freed.
    /// </summary>
    public void Dispose()
    {
        _lock.EnterWriteLock();
        try
        {
            if (_disposed)
            {
                _logger.LogDebug("Device '{DeviceKey}' already disposed.", Descriptor.DeviceKey);
                return;
            }

            // Release all claimed USB interfaces
            foreach (var usbInterface in _claimedInterfaces.Values)
            {
                usbInterface.Dispose();
                var releaseResult = libusb_release_interface(Handle, usbInterface.Number);
                if (releaseResult == 0)
                {
                    _logger.LogDebug("USB interface {UsbInterface} released.", usbInterface);
                    continue;
                }
                _logger.LogError(
                    "Failed to release USB interface {UsbInterface}. {ErrorMessage}",
                    usbInterface,
                    ((LibUsbResult)releaseResult).GetMessage()
                );
            }
            _claimedInterfaces.Clear();

            // Ask LibUsb to close device and remove it from list of open devices
            _libUsb.CloseDevice(Descriptor.DeviceKey, Handle);

            _disposed = true;
            _logger.LogInformation("UsbDevice '{DeviceKey}' disposed.", Descriptor.DeviceKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "UsbDevice dispose failed. {ErrorType}: {ErrorMessage}",
                ex.GetType().Name,
                ex.Message
            );
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // LibraryImportAttribute not available in .NET6, silence warning
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'

    /// <summary>
    /// Wrapper around libusb_get_string_descriptor(). Uses the first language supported by the
    /// device. The function formulates the appropriate control message to retrieve the descriptor,
    /// and converts the Unicode string returned by the device to ASCII.
    /// </summary>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_get_string_descriptor_ascii(
        IntPtr deviceHandle,
        byte descIndex,
        byte[] data,
        int length
    );

    /// <summary>
    /// Claim an interface on a given device handle. You must claim the interface you wish to use
    /// before you can perform I/O on any of its endpoints. It is legal to attempt to claim an
    /// already-claimed interface, in which case libusb just returns 0 without doing anything.
    /// If auto_detach_kernel_driver is set to 1 for dev, the kernel driver will be detached
    /// if necessary, on failure the detach error is returned. Claiming of interfaces is a purely
    /// logical operation; it does not cause any requests to be sent over the bus.Interface claiming
    /// is used to instruct the underlying operating system that your application wishes to take
    /// ownership of the interface.
    /// </summary>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_claim_interface(IntPtr deviceHandle, int interfaceNumber);

    /// <summary>
    /// Release an interface previously claimed with libusb_claim_interface(). You should release
    /// all claimed interfaces before closing a device handle. This is a blocking function.
    /// A SET_INTERFACE control request will be sent to the device, resetting interface state to the
    /// first alternate setting. If auto_detach_kernel_driver is set to 1 for dev, the kernel driver
    /// will be re-attached after releasing the interface.
    /// </summary>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_release_interface(IntPtr deviceHandle, int interfaceNumber);

    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_reset_device(IntPtr deviceHandle);

#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'
}
