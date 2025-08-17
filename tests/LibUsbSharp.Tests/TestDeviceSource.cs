using System.Diagnostics.CodeAnalysis;
using LibUsbSharp.Descriptor;
using Microsoft.Extensions.Logging;
using Xunit;

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

    /// <summary>
    /// Returns an open USB device or throws an exception that results
    /// in a "Skipped" result for the test, when no device is found.
    /// </summary>
    public IUsbDevice OpenUsbDeviceOrSkip(UsbClass? withInterfaceClass = null)
    {
        if (TryOpenUsbDevice(out var openDevice, withInterfaceClass))
        {
            return openDevice;
        }
        throw withInterfaceClass is null
            ? new SkipException("No USB device available.")
            : new SkipException($"No USB device with a {withInterfaceClass} interface available.");
    }

    public bool TryOpenUsbDevice(
        [NotNullWhen(true)] out IUsbDevice? openDevice,
        UsbClass? withInterfaceClass = null
    )
    {
        var devices = _libUsb
            .GetDeviceList(_requiredVendorId)
            .OrderBy(d => d.VendorId == _preferredVendorId ? 0 : 1);

        foreach (var deviceDescriptor in devices)
        {
            if (TryOpenDevice(deviceDescriptor, withInterfaceClass, out openDevice))
            {
                return true;
            }
        }

        openDevice = null;
        return false;
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
            && DeviceSerialIsReadable(device)
            && (
                withInterfaceClass is null
                || DeviceInterfaceIsAccessible(device, withInterfaceClass.Value)
            )
        )
        {
            openDevice = device;
            return true;
        }
        device?.Dispose();
        openDevice = null;
        return false;
    }

    private static bool DeviceInterfaceIsAccessible(IUsbDevice device, UsbClass interfaceClass)
    {
        if (device.HasInterface(interfaceClass))
        {
            try
            {
                using var usbInterface = device.ClaimInterface(interfaceClass);
                return usbInterface.TryGetInputEndpoint(out _)
                    && usbInterface.TryGetOutputEndpoint(out _);
            }
            catch
            {
                // Interface claim failed - device is not accessible
            }
        }
        return false;
    }

    private bool DeviceSerialIsReadable(IUsbDevice device)
    {
        var serialNumberIndex = device.Descriptor.SerialNumberIndex;
        if (serialNumberIndex == 0)
        {
            _logger.LogInformation(
                "Device '{DeviceKey}' serial number index is 0, aka 'no string provided'.",
                device.Descriptor.DeviceKey
            );
            return false;
        }
        try
        {
            var serialNumber = device.ReadStringDescriptor(serialNumberIndex);
            _logger.LogInformation(
                "Device '{DeviceKey}' has serial number '{SerialNumber}'.",
                device.Descriptor.DeviceKey,
                serialNumber
            );
            return true;
        }
        catch (LibUsbException ex)
        {
            _logger.LogInformation(
                "Device '{DeviceKey}' serial number not readable. {ErrorMessage}",
                device.Descriptor.DeviceKey,
                ex.Message
            );
            return false;
        }
    }
}
