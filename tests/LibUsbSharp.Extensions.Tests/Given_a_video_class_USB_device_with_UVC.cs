using LibUsbSharp.Extensions.Uvc;
using LibUsbSharp.Transfer;

namespace LibUsbSharp.Extensions.Tests;

[Trait("Category", "UsbVideoControl")]
public sealed class Given_a_video_class_USB_device_with_UVC : IDisposable
{
    private const byte UvcInterfaceSubClass = 0x01; // SC_VIDEOCONTROL

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Given_a_video_class_USB_device_with_UVC> _logger;
    private readonly LibUsb _libUsb;
    private readonly TestDeviceSource _deviceSource;

    public Given_a_video_class_USB_device_with_UVC(ITestOutputHelper output)
    {
        _loggerFactory = new TestLoggerFactory(output);
        _logger = _loggerFactory.CreateLogger<Given_a_video_class_USB_device_with_UVC>();
        _libUsb = new LibUsb(_loggerFactory);
        _libUsb.Initialize(LogLevel.Information);
        _deviceSource = new TestDeviceSource(_logger, _libUsb);
        _deviceSource.SetPreferredVendorId(0x2BD9);
        _deviceSource.SetRequiredInterfaceClass(UsbClass.Video, TestDeviceAccess.Control);
        _deviceSource.SetRequiredInterfaceSubClass(UvcInterfaceSubClass);
    }

    [SkippableFact]
    public void Device_with_UVC_support_found()
    {
        using var device = _deviceSource.OpenUsbDeviceOrSkip();
        var serial = device.GetSerialNumber();
        _logger.LogInformation(
            "Video device w/UVC open: VID=0x{VID:X4}, PID=0x{PID:X4}, SerialNumber={SerialNumber}.",
            device.Descriptor.VendorId,
            device.Descriptor.ProductId,
            serial
        );
        var selector = 0x02;
        var processingUnit = 0x03;
        var vcInterface = 0x00;
        var value = (ushort)(selector << 8);
        var index = (ushort)(processingUnit << 8 | vcInterface);
        var val = new Span<byte>(new byte[2]);
        var result = device.ControlUvcRead(
            ControlRequestRecipient.Interface,
            ControlRequestUvc.GetCurrentSetting,
            value,
            index,
            val,
            out _,
            1000
        );
        //var valueConverted = BitConverter.ToInt16(val);
        result.Should().Be(LibUsbResult.Success);

        // TODO: This test is an example; replace with a real UVC device test method
        //var result = device.ControlUvcWrite(
        //    ControlRequestRecipient.Device,
        //    ControlRequestUvc.SetCurrentSetting,
        //    0,
        //    0,
        //    [],
        //    out var bytesWritten
        //);
        //result.Should().Be(LibUsbResult.Success);
    }

    public void Dispose()
    {
        _libUsb.Dispose();
    }
}
