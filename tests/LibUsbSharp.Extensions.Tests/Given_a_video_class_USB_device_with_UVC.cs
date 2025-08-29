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
    private const ushort value = selector << 8;
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
        var uvcInterfaces = device.GetInterfaceDescriptors(UsbClass.Video, UvcInterfaceSubClass);
        var uvcInterface = uvcInterfaces.First();
        _logger.LogInformation(
            "Video device open: VID=0x{VID:X4}, PID=0x{PID:X4}, "
                + "SerialNumber={SerialNumber}, UVC interface: {Interface}.",
            device.Descriptor.VendorId,
            device.Descriptor.ProductId,
            serial,
            uvcInterface.InterfaceNumber
        );
        var val = new Span<byte>(new byte[2]);
        var result = device.ControlUvcRead(
            ControlRequestRecipient.Interface,
            ControlRequestUvc.GetCurrentSetting,
            value,
            (ushort)(processingUnit << 8 | uvcInterface.InterfaceNumber),
            val,
            out _,
            1000
        );
        var valueConverted = BitConverter.ToInt16(val);
        result.Should().Be(LibUsbResult.Success);
    }

    [SkippableFact]
    public void ControUvcWrite_Brightness_should_complete_successfully()
    {
        using var device = _deviceSource.OpenUsbDeviceOrSkip();
        var serial = device.GetSerialNumber();
        var uvcInterface = device.GetInterfaceDescriptors(UsbClass.Video, UvcInterfaceSubClass).First();
        _logger.LogInformation(
            "Video device open: VID=0x{VID:X4}, PID=0x{PID:X4}, "
                + "SerialNumber={SerialNumber}, UVC interface: {Interface}.",
            device.Descriptor.VendorId,
            device.Descriptor.ProductId,
            serial,
            uvcInterface.InterfaceNumber
        );
        var initialValSpan = new Span<byte>(new byte[2]);
        _ = device.ControlUvcRead(
            ControlRequestRecipient.Interface,
            ControlRequestUvc.GetCurrentSetting,
            value,
            (ushort)(processingUnit << 8 | uvcInterface.InterfaceNumber),
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
            (ushort)(processingUnit << 8 | uvcInterface.InterfaceNumber),
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
            (ushort)(processingUnit << 8 | uvcInterface.InterfaceNumber),
            newValSpan,
            out _,
            1000
        );

        newVal.Should().Be(BitConverter.ToInt16(newValSpan.ToArray()));
    }

    public void Dispose()
    {
        _libUsb.Dispose();
    }
}
