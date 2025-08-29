using LibUsbSharp.Extensions.Uvc;
using LibUsbSharp.Transfer;

namespace LibUsbSharp.Extensions.Tests;

[Trait("Category", "UsbVideoControl")]
public sealed class Given_a_video_class_USB_device_with_UVC : IDisposable
{
    static byte[] FromI16LE(int v) => new[] { (byte)(v & 0xFF), (byte)((v >> 8) & 0xFF) };

    private const byte UvcInterfaceSubClass = 0x01; // SC_VIDEOCONTROL
    private const byte selector = 0x02;
    private const byte processingUnit = 0x03;
    private const byte vcInterface = 0x00;
    private const ushort value = (ushort)(selector << 8);
    private const ushort index = (ushort)(processingUnit << 8 | vcInterface);
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
    public void ControUvcRead_Brightness_should_complete_successfully()
    {
        using var device = _deviceSource.OpenUsbDeviceOrSkip();
        var serial = device.GetSerialNumber();
        _logger.LogInformation(
            "Video device w/UVC open: VID=0x{VID:X4}, PID=0x{PID:X4}, SerialNumber={SerialNumber}.",
            device.Descriptor.VendorId,
            device.Descriptor.ProductId,
            serial
        );
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

    [SkippableFact]
    public void ControUvcWrite_Brightness_should_complete_successfully()
    {
        using var device = _deviceSource.OpenUsbDeviceOrSkip();
        var serial = device.GetSerialNumber();
        _logger.LogInformation(
            "Video device w/UVC open: VID=0x{VID:X4}, PID=0x{PID:X4}, SerialNumber={SerialNumber}.",
            device.Descriptor.VendorId,
            device.Descriptor.ProductId,
            serial
        );
        var initialValSpan = new Span<byte>(new byte[2]);
        _ = device.ControlUvcRead(
            ControlRequestRecipient.Interface,
            ControlRequestUvc.GetCurrentSetting,
            value,
            index,
            initialValSpan,
            out _,
            1000
        );

        var initialVal = BitConverter.ToInt16(initialValSpan);
        var newVal = initialVal > 400 ? -600 : initialVal + 150;
        var val = new Span<byte>(FromI16LE(newVal));
        var result = device.ControlUvcWrite(
            ControlRequestRecipient.Interface,
            ControlRequestUvc.SetCurrentSetting,
            value,
            index,
            val,
            out _,
            1000
        );
        //var valueConverted = BitConverter.ToInt16(val);
        result.Should().Be(LibUsbResult.Success);

        var newValSpan = new Span<byte>(new byte[2]);
        _ = device.ControlUvcRead(
            ControlRequestRecipient.Interface,
            ControlRequestUvc.GetCurrentSetting,
            value,
            index,
            newValSpan,
            out _,
            1000
        );

        newVal.Should().Be(BitConverter.ToInt16(newValSpan.ToArray()));
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
