using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using LibUsbNative.SafeHandles;
using LibUsbSharp.Descriptor;
using LibUsbSharp.Internal;
using LibUsbSharp.Internal.Transfer;
using LibUsbSharp.Transfer;
using Microsoft.Extensions.Logging;

namespace LibUsbSharp;

public sealed class UsbDevice : IUsbDevice
{
    private const byte ControlRequestEndpoint = 0x00;

    private readonly LibUsb _libUsb;
    private readonly ISafeContext _context;
    private readonly UsbDeviceDescriptor _descriptor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<UsbDevice> _logger;
    private readonly ConcurrentDictionary<byte, UsbInterface> _claimedInterfaces = new();
    private readonly ConcurrentDictionary<byte, string> _descriptorCache = new();
    private readonly object _cacheLock = new();
    private readonly RundownGuard _rundownGuard = new();
    private readonly object _interfaceLock = new();
    private readonly CancellationTokenSource _disposeCts = new();

    internal ISafeDeviceHandle Handle { get; init; }

    /// <inheritdoc />
    public IUsbDeviceDescriptor Descriptor => _descriptor;

    /// <inheritdoc />
    public IUsbConfigDescriptor ConfigDescriptor { get; init; }

    internal UsbDevice(
        ILoggerFactory loggerFactory,
        LibUsb libUsb,
        ISafeContext context,
        ISafeDeviceHandle handle,
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
        using var token = _rundownGuard.AcquireSharedToken();

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
        using var token = _rundownGuard.AcquireSharedToken();

        var buffer = new byte[256];

        return Handle.GetStringDescriptorAscii(descriptorIndex);
    }

    /// <inheritdoc />
    public LibUsbResult ControlRead(
        ControlRequestRecipient recipient,
        ControlRequestType type,
        byte request,
        ushort value,
        ushort index,
        Span<byte> destination,
        out ushort bytesRead,
        int timeout
    )
    {
        if (destination.Length > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(destination),
                destination.Length,
                $"Destination buffer must be less than {ushort.MaxValue} bytes."
            );
        }

        using var token = _rundownGuard.AcquireSharedToken();

        var length = (ushort)destination.Length;
        var buffer = ControlRequestPacket.CreateRead(recipient, type, request, value, index, length);
        var bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var result = LibUsbTransfer.ExecuteSync(
                _logger,
                Handle,
                LibUsbTransferType.Control,
                ControlRequestEndpoint,
                bufferHandle,
                buffer.Length,
                timeout > 0 ? (uint)timeout : 0,
                out var bytesReadInt, // Length of data only (not setup)
                _disposeCts.Token
            );
            bytesRead = (ushort)bytesReadInt;
            if (result != LibUsbResult.Success || bytesRead <= 0)
            {
                destination = Array.Empty<byte>();
                return result;
            }
            buffer.AsSpan(ControlRequestPacket.SetupSize, bytesRead).CopyTo(destination);
            return result;
        }
        finally
        {
            bufferHandle.Free();
        }
    }

    /// <inheritdoc />
    public LibUsbResult ControlWrite(
        ControlRequestRecipient recipient,
        ControlRequestType type,
        byte request,
        ushort value,
        ushort index,
        ReadOnlySpan<byte> source,
        out int bytesWritten,
        int timeout
    )
    {
        if (source.Length > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source.Length,
                $"Payload must be less than {ushort.MaxValue} bytes."
            );
        }

        using var token = _rundownGuard.AcquireSharedToken();

        var length = (ushort)source.Length;
        var buffer = ControlRequestPacket.CreateWrite(recipient, type, request, value, index, length);
        if (length > 0)
        {
            source.CopyTo(buffer.AsSpan(ControlRequestPacket.SetupSize, length));
        }
        var bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            return LibUsbTransfer.ExecuteSync(
                _logger,
                Handle,
                LibUsbTransferType.Control,
                ControlRequestEndpoint,
                bufferHandle,
                buffer.Length,
                timeout > 0 ? (uint)timeout : 0,
                out bytesWritten, // Length of data only (not setup)
                _disposeCts.Token
            );
        }
        finally
        {
            bufferHandle.Free();
        }
    }

    /// <inheritdoc />
    public IUsbInterface ClaimInterface(IUsbInterfaceDescriptor descriptor)
    {
        using var token = _rundownGuard.AcquireExclusiveToken();

        lock (_interfaceLock)
        {
            if (_claimedInterfaces.TryGetValue(descriptor.InterfaceNumber, out var existing))
            {
                throw new InvalidOperationException($"USB interface {existing} already claimed.");
            }

            // TODO: libusb_set_auto_detach_kernel_driver on Linux?
            var claimedInterface = Handle.ClaimInterface(descriptor.InterfaceNumber);

            var usbInterface = new UsbInterface(_loggerFactory, this, descriptor, claimedInterface);
            // No need to check if already added, checked in TryGetValue above
            _claimedInterfaces[descriptor.InterfaceNumber] = usbInterface;
            _logger.LogDebug("USB interface {UsbInterface} claimed.", usbInterface);
            return usbInterface;
        }
    }

    /// <summary>
    /// Release a USB interface. NOTE: Only used internally, called from UsbInterface.Dispose().
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the USB interface is not claimed.
    /// </exception>
    /// <exception cref="LibUsbException">
    /// Thrown when the USB interface release operation fails.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the UsbDevice is disposed.
    /// </exception>
    internal void ReleaseInterface(byte interfaceNumber)
    {
        lock (_interfaceLock)
        {
            if (!_claimedInterfaces.TryGetValue(interfaceNumber, out var usbInterface))
            {
                throw new InvalidOperationException(
                    $"USB interface #{interfaceNumber} not found in list of claimed interfaces."
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
    }

    /// <inheritdoc />
    public void Reset()
    {
        using var token = _rundownGuard.AcquireExclusiveToken();

        var resetResult = (int)Handle.ResetDevice();
        if (resetResult != 0)
        {
            throw LibUsbException.FromError(resetResult, $"Failed to reset USB device port.");
        }
    }

    public override string ToString() => _descriptor.DeviceKey;

    /// <summary>
    /// Disposes this device and associated resources. Ongoing transfers are canceled,
    /// claimed interfaces are automatically released and allocated memory is freed.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _disposeCts.Cancel();
            _rundownGuard.Dispose();
        }
        catch (ObjectDisposedException)
        {
#if DEBUG
            throw;
#else
            _logger.LogWarning("UsbDevice already disposed.");
            return;
#endif
        }
        try
        {
            lock (_interfaceLock)
            {
                // Release all claimed USB interfaces
                foreach (var usbInterface in _claimedInterfaces.Values)
                {
                    usbInterface.Dispose();
                }
                _claimedInterfaces.Clear();
            }
            // Ask LibUsb to close device and remove it from list of open devices
            _libUsb.CloseDevice(Descriptor.DeviceKey, Handle);
            _logger.LogInformation("UsbDevice '{DeviceKey}' disposed.", Descriptor.DeviceKey);

            _disposeCts.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError("UsbDevice dispose failed. {ErrorType}: {ErrorMessage}", ex.GetType().Name, ex.Message);
        }
    }
}
