using FluentAssertions;
using LibUsbNative.Enums;
using LibUsbNative.SafeHandles;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.SafeHandles.SafeDevice;

public class Given_an_accessible_USB_device : SafeHandlesTestBase
{
    private readonly ITestOutputHelper output;
    private readonly ISafeContext context;
    private readonly List<string> stdout = [];
    private readonly LibUsbNative libUsb;

    public Given_an_accessible_USB_device(ITestOutputHelper output)
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
    public void TestFailsAfterDispose()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();

            var device = list.Devices.ToList()[0];
            list.Dispose();

            Action act = () => device.GetActiveConfigDescriptor();
            act.Should().Throw<ObjectDisposedException>();

            // TODO: Picks random device to open and fails. In some cases this results in:
            // Failed to open USB device. Operation not supported or unimplemented on this platform.
            act = () => device.Open();
            act.Should().Throw<ObjectDisposedException>();

            act = () => device.GetBusNumber();
            act.Should().Throw<ObjectDisposedException>();

            act = () => device.GetDeviceAddress();
            act.Should().Throw<ObjectDisposedException>();

            act = () => device.GetPortNumber();
            act.Should().Throw<ObjectDisposedException>();

            act = () => device.GetDeviceDescriptor();
            act.Should().Throw<ObjectDisposedException>();
        });
    }
};
