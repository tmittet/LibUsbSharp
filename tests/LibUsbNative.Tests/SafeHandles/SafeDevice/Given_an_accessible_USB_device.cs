using FluentAssertions;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.SafeHandles.SafeDevice;

[Trait("Category", "UsbDevice")]
public class Given_an_accessible_USB_device_Real(ITestOutputHelper output)
    : Given_an_accessible_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_an_accessible_USB_device(ITestOutputHelper output, ILibUsbApi api)
    : LibUsbNativeTestBase(output, api)
{
    [Fact]
    public void TestFailsAfterDispose()
    {
        EnterReadLock(() =>
        {
            var context = GetContext();
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
