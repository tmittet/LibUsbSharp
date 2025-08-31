using LibUsbSharp.Extensions.ControlTransfer;
using LibUsbSharp.Extensions.ControlTransfer.Uvc;

namespace LibUsbSharp.Extensions.Tests.ControlTransfer.Uvc;

[Trait("Category", "UsbVideoControl")]
public sealed class Given_a_video_class_USB_device_with_UVC : IDisposable
{
    private const byte UvcInterfaceSubClass = 0x01; // SC_VIDEOCONTROL
    private const byte Selector = 0x02;
    private const byte ProcessingUnit = 0x03;
    private const ushort Value = Selector << 8;

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
        var result = device.ControlRead(
            ControlRequestUvc.Interface.Class(
                UvcRequest.GetCurrentSetting,
                uvcInterface.InterfaceNumber,
                ProcessingUnit,
                Value
            ),
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
        _ = device.ControlRead(
            ControlRequestUvc.Interface.Class(
                UvcRequest.GetCurrentSetting,
                uvcInterface.InterfaceNumber,
                ProcessingUnit,
                Value
            ),
            initialValSpan,
            out _,
            1000
        );

        var initialVal = BitConverter.ToInt16(initialValSpan);
        var newVal = initialVal > 400 ? -600 : initialVal + 150;
        var val = new Span<byte>(BitConverter.GetBytes(newVal));
        var result = device.ControlWrite(
            ControlRequestUvc.Interface.Class(
                UvcRequest.SetCurrentSetting,
                uvcInterface.InterfaceNumber,
                ProcessingUnit,
                Value
            ),
            val,
            out _,
            1000
        );

        result.Should().Be(LibUsbResult.Success);

        var newValSpan = new Span<byte>(new byte[2]);
        _ = device.ControlRead(
            ControlRequestUvc.Interface.Class(
                UvcRequest.GetCurrentSetting,
                uvcInterface.InterfaceNumber,
                ProcessingUnit,
                Value
            ),
            newValSpan,
            out _,
            1000
        );

        newVal.Should().Be(BitConverter.ToInt16(newValSpan));
    }

    public void Dispose()
    {
        _libUsb.Dispose();
    }
}
