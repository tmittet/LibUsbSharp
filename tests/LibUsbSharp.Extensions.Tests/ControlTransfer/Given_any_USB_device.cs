using LibUsbSharp.Extensions.ControlTransfer;

namespace LibUsbSharp.Extensions.Tests.ControlTransfer;

[Trait("Category", "UsbDevice")]
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

    [SkippableFact]
    public void ControlRead_returns_expected_descriptor_given_Standard_GetDescriptor_request()
    {
        const byte DescriptorTypeDevice = 0x01;
        const ushort DescriptorIndex = 0x00;

        using var device = _deviceSource.OpenUsbDeviceOrSkip();

        // Allocate a slightly bigger buffer than the expected 18 bytes,
        // this enables us to test that bytesRead returns expected length.
        var descriptorBuffer = new byte[32];

        var result = device.ControlRead(
            ControlRequest.Device.Standard(
                StandardRequest.GetDescriptor,
                (DescriptorTypeDevice << 8) | DescriptorIndex,
                DescriptorIndex
            ),
            descriptorBuffer,
            out var bytesRead
        );

        using var scope = new AssertionScope();
        result.Should().Be(LibUsbResult.Success);

        // USB Descriptor is always 18 bytes
        bytesRead.Should().Be(18);
        // Byte 4 is device class
        ((UsbClass)descriptorBuffer[4])
            .Should()
            .Be(device.Descriptor.DeviceClass);
        // Byte 8-9 is vendor ID
        BitConverter.ToUInt16(descriptorBuffer[8..10], 0).Should().Be(device.Descriptor.VendorId);
        // Byte 10-11 is product ID
        BitConverter.ToUInt16(descriptorBuffer[10..12], 0).Should().Be(device.Descriptor.ProductId);
    }

    [SkippableFact]
    public void ControlWrite_is_successfull_given_Standard_SetConfiguration_request()
    {
        const ushort DescriptorIndex = 0;

        using var device = _deviceSource.OpenUsbDeviceOrSkip();

        // Start by getting current device configuration
        var readBuffer = new byte[1];
        var readResult = device.ControlRead(
            ControlRequest.Device.Standard(StandardRequest.GetConfiguration),
            readBuffer,
            out var bytesRead
        );
        if (readResult != LibUsbResult.Success && bytesRead == 1)
        {
            throw new SkipException($"ControlRead result '{readResult}', {bytesRead} bytes read.");
        }

        // When configuration read is successful, write the same config value back to the device
        var writeResult = device.ControlWrite(
            ControlRequest.Device.Standard(StandardRequest.SetConfiguration, readBuffer[0], DescriptorIndex),
            [],
            out var bytesWritten
        );

        using var scope = new AssertionScope();
        writeResult.Should().Be(LibUsbResult.Success);
        // We did not provide a payload, expect zero bytes written
        bytesWritten.Should().Be(0);
    }

    public void Dispose()
    {
        _libUsb.Dispose();
    }
}
