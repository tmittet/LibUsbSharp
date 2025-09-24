using FluentAssertions;
using LibUsbNative.Enums;
using LibUsbNative.SafeHandles;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.SafeHandles.SafeDevice;

public class Given_no_USB_device : SafeHandlesTestBase
{
    private readonly ITestOutputHelper output;
    private readonly ISafeContext context;
    private readonly List<string> stdout = [];
    private readonly LibUsbNative libUsb;

    public Given_no_USB_device(ITestOutputHelper output)
    {
        this.output = output;
        libUsb = new LibUsbNative(new PInvokeLibUsbApi());

        var version = libUsb.GetVersion();
        output.WriteLine(version.ToString());

        context = libUsb.CreateContext();

        context.RegisterLogCallback(
            (level, message) =>
            {
                output.WriteLine($"[Libusb][{level}] {message}");
                stdout.Add(message);
            }
        );

        context.SetOption(libusb_option.LIBUSB_OPTION_LOG_LEVEL, 3);
    }

    [Fact]
    public void TestGetDeviceDescriptor()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();

            var device = list.Devices.ToList()[0];
            var descriptor = device.GetDeviceDescriptor();
            descriptor.bDescriptorType.Should().Be(libusb_descriptor_type.LIBUSB_DT_DEVICE);

            list.Dispose();
        });
    }

    [Fact]
    public void TestGetActiveConfigDescriptor()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();

            var device = list.Devices.ToList()[0];
            var descriptor = device.GetActiveConfigDescriptor();
            descriptor.bDescriptorType.Should().Be(libusb_descriptor_type.LIBUSB_DT_CONFIG);

            list.Dispose();
        });
    }
};
