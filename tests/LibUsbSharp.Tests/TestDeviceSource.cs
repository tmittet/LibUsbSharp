using System.Diagnostics.CodeAnalysis;
using LibUsbSharp.Descriptor;
using Microsoft.Extensions.Logging;

namespace LibUsbSharp.Tests;

public class TestDeviceSource(ILogger _logger, ILibUsb _libUsb)
{
    private ushort _preferredVendorId;
    private ushort? _requiredVendorId;

    public void SetPreferredVendorId(ushort vendorId)
    {
        _preferredVendorId = vendorId;
    }

    public void SetRequiredVendorId(ushort vendorId)
    {
        _requiredVendorId = vendorId;
    }

    public IUsbDevice OpenAccessibleUsbDevice(UsbClass? withInterfaceClass = null)
    {
        var devices = _libUsb
            .GetDeviceList(_requiredVendorId)
            .OrderBy(d => d.VendorId == _preferredVendorId ? 0 : 1);

        foreach (var deviceDescriptor in devices)
        {
            if (TryOpenDevice(deviceDescriptor, withInterfaceClass, out var device))
            {
                return device;
            }
        }

        throw withInterfaceClass is null
            ? new InvalidOperationException("Accessible USB device interface not found.")
            : new InvalidOperationException(
                $"Accessible USB device with {withInterfaceClass} interface not found."
            );
    }

    public bool TryOpenDevice(
        IUsbDeviceDescriptor deviceDescriptor,
        UsbClass? withInterfaceClass,
        [NotNullWhen(true)] out IUsbDevice? openDevice,
        int attempts = 3
    )
    {
        IUsbDevice? device = null;
        for (var i = 0; device is null && i < attempts; i++)
        {
            try
            {
                device = _libUsb.OpenDevice(deviceDescriptor);
            }
            catch (LibUsbException ex)
                when (ex.ResultCode
                        is LibUsbResult.AccessDenied
                            or LibUsbResult.IoError
                            or LibUsbResult.NotSupported
                )
            {
                if (i > 0)
                {
                    Thread.Sleep(10);
                }
                _logger.LogInformation(
                    "Device '{DeviceKey}' not accessible on attempt #{Attempt}. {ErrorCode}: {ErrorMessage}",
                    deviceDescriptor.DeviceKey,
                    i + 1,
                    ex.ResultCode,
                    ex.Message
                );
            }
        }
        if (
            device is not null
            && (withInterfaceClass is null || device.HasInterface(withInterfaceClass.Value))
        )
        {
            openDevice = device;
            return true;
        }
        device?.Dispose();
        openDevice = null;
        return false;
    }
}
