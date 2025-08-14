using FluentAssertions;
using LibUsbSharp.Descriptor;
using LibUsbSharp.Tests.TestLogger;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LibUsbSharp.Tests;

public sealed class Given_any_USB_device : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Given_any_USB_device> _logger;
    private readonly LibUsb _libUsb;
    private readonly TestDeviceSource _deviceSource;

    public Given_any_USB_device(ITestOutputHelper output)
    {
        _loggerFactory = new TestLoggerFactory(output);
        _logger = _loggerFactory.CreateLogger<Given_any_USB_device>();
        _libUsb = new LibUsb(_loggerFactory);
        _libUsb.Initialize(LogLevel.Information);
        _deviceSource = new TestDeviceSource(_logger, _libUsb);
        _deviceSource.SetPreferredVendorId(0x2BD9);
    }

    [Fact(Skip = "No USB devices available on public GitHub runner")]
    public void GetDeviceList_returns_at_least_one_USB_device()
    {
        var descriptors = _libUsb.GetDeviceList();
        descriptors.Should().HaveCountGreaterThanOrEqualTo(1);
        foreach (var descriptor in descriptors)
        {
            _logger.LogInformation(
                "Device found: Class={DeviceClass}, VID=0x{VID:X4}, PID=0x{PID:X4}, "
                    + "BusNumber={BusNumber}, BusAddress={BusAddress}, PortNumber={PortNumber}.",
                descriptor.DeviceClass,
                descriptor.VendorId,
                descriptor.ProductId,
                descriptor.BusNumber,
                descriptor.BusAddress,
                descriptor.PortNumber
            );
        }
    }

    [Fact(Skip = "No USB devices available on public GitHub runner")]
    public void GetDeviceSerial_returns_serial_given_a_device_descriptor_when_device_is_not_open()
    {
        IUsbDeviceDescriptor deviceDescriptor;
        using (var device = _deviceSource.OpenAccessibleUsbDevice())
        {
            deviceDescriptor = device.Descriptor;
        }
        var serial = _libUsb.GetDeviceSerial(deviceDescriptor);
        serial.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "No USB devices available on public GitHub runner")]
    public void GetDeviceSerial_succeeds_given_a_device_descriptor_when_device_is_already_open()
    {
        using var openDevice = _deviceSource.OpenAccessibleUsbDevice();
        _logger.LogInformation(
            "Device open: VID=0x{VID:X4}, PID=0x{PID:X4}, SerialNumber={SerialNumber}.",
            openDevice.Descriptor.VendorId,
            openDevice.Descriptor.ProductId,
            openDevice.GetSerialNumber()
        );
        // Get serial using the descriptor (not the open device)
        var serial = _libUsb.GetDeviceSerial(openDevice.Descriptor);
        serial.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "No USB devices available on public GitHub runner")]
    public void GetSerialNumber_returns_serial_given_an_open_device()
    {
        using var device = _deviceSource.OpenAccessibleUsbDevice();
        var serial = device.GetSerialNumber();
        _logger.LogInformation(
            "Device open: VID=0x{VID:X4}, PID=0x{PID:X4}, SerialNumber={SerialNumber}.",
            device.Descriptor.VendorId,
            device.Descriptor.ProductId,
            serial
        );
        serial.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "No USB devices available on public GitHub runner")]
    public void GetManufacturer_returns_manufacturer_given_an_open_device()
    {
        using var device = _deviceSource.OpenAccessibleUsbDevice();
        var manufacturer = device.GetManufacturer();
        manufacturer.Should().NotBeNullOrEmpty();
        _ = device.GetManufacturer();
    }

    [Fact(Skip = "No USB devices available on public GitHub runner")]
    public void GetProductName_returns_product_name_given_an_open_device()
    {
        using var device = _deviceSource.OpenAccessibleUsbDevice();
        var productName = device.GetProduct();
        productName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "No USB devices available on public GitHub runner")]
    public void Open_devices_are_auto_disposed_when_LibUsb_is_disposed()
    {
        // Open device and leave it open
        var device = _deviceSource.OpenAccessibleUsbDevice();
        // Dispose LibUsb to trigger auto disposal of devices
        _libUsb.Dispose();
        // Attempt to get serial, the device should be auto disposed at this point
        var act = () => device.GetSerialNumber();
        act.Should().Throw<ObjectDisposedException>();
        // Calling dispose again is OK
        device.Dispose();
    }

    public void Dispose()
    {
        _libUsb.Dispose();
    }
}
