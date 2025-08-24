using LibUsbSharp.Descriptor;

namespace LibUsbSharp.Tests;

[Trait("Category", "UsbVendorClassDevice")]
public sealed class Given_a_vendor_class_USB_device : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Given_a_vendor_class_USB_device> _logger;
    private readonly LibUsb _libUsb;
    private readonly TestDeviceSource _deviceSource;

    public Given_a_vendor_class_USB_device(ITestOutputHelper output)
    {
        _loggerFactory = new TestLoggerFactory(output);
        _logger = _loggerFactory.CreateLogger<Given_a_vendor_class_USB_device>();
        _libUsb = new LibUsb(_loggerFactory);
        _libUsb.Initialize(LogLevel.Information);
        _deviceSource = new TestDeviceSource(_logger, _libUsb);
        _deviceSource.SetPreferredVendorId(0x2BD9);
        _deviceSource.SetRequiredInterfaceClass(
            UsbClass.VendorSpecific,
            TestDeviceAccess.BulkRead | TestDeviceAccess.BulkWrite
        );
    }

    [SkippableFact]
    public void Device_has_vendor_interface_with_input_and_output_endpoints()
    {
        using var device = _deviceSource.OpenUsbDeviceOrSkip();
        device
            .ConfigDescriptor.Interfaces.Should()
            .ContainSingle(i => i.InterfaceClass == UsbClass.VendorSpecific);
        var vendorInterface = device.ConfigDescriptor.Interfaces.First(i =>
            i.InterfaceClass == UsbClass.VendorSpecific
        );
        vendorInterface.GetEndpoint(UsbEndpointDirection.Input, out var inputCount);
        inputCount.Should().BeGreaterThanOrEqualTo(1);
        vendorInterface.GetEndpoint(UsbEndpointDirection.Output, out var outputCount);
        outputCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [SkippableFact]
    public void Device_is_able_to_claim_interface_and_get_an_input_endpoint()
    {
        using var device = _deviceSource.OpenUsbDeviceOrSkip();
        using var usbInterface = device.ClaimInterface(UsbClass.VendorSpecific);
        var endpointFound = usbInterface.TryGetInputEndpoint(out var endpoint);
        endpointFound.Should().BeTrue();
        endpoint!.MaxPacketSize.Should().BePositive();
    }

    [SkippableFact]
    public void Device_is_able_to_claim_interface_and_get_an_output_endpoint()
    {
        using var device = _deviceSource.OpenUsbDeviceOrSkip();
        using var usbInterface = device.ClaimInterface(UsbClass.VendorSpecific);
        var endpointFound = usbInterface.TryGetOutputEndpoint(out var endpoint);
        endpointFound.Should().BeTrue();
        endpoint!.MaxPacketSize.Should().BePositive();
    }

    [SkippableFact]
    public void Open_interfaces_are_auto_disposed_when_UsbDevice_is_disposed()
    {
        // Open device and leave it open
        var device = _deviceSource.OpenUsbDeviceOrSkip();
        // Claim interface without disposing it
        _ = device.ClaimInterface(UsbClass.VendorSpecific);
        // Dispose device
        device.Dispose();
    }

    public void Dispose()
    {
        _libUsb.Dispose();
    }
}
