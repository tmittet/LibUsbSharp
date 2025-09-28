namespace LibUsbSharp.Native.Tests.SafeHandles.SafeContext;

public class Given_no_USB_device_Fake(ITestOutputHelper output) : Given_no_USB_device(output, new FakeLibusbApi());

public class Given_no_USB_device_Real(ITestOutputHelper output) : Given_no_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_no_USB_device(ITestOutputHelper output, ILibUsbApi api) : LibUsbNativeTestBase(output, api)
{
    [Fact]
    public void TestNoListDispose()
    {
        EnterReadLock(() =>
        {
            var context = GetContext();
            var list = context.GetDeviceList();
            context.Dispose();
            _ = LibUsbOutput.Should().NotContain(s => s.Contains("still referenced"));
        });
    }

    [Fact]
    public void TestFailsAfterDispose()
    {
        EnterReadLock(() =>
        {
            var context = GetContext();
            context.Dispose();

            Action act = () => context.GetDeviceList();
            act.Should().Throw<ObjectDisposedException>();

            act = () =>
            {
                context.RegisterLogCallback((level, message) => { });
            };
            act.Should().Throw<ObjectDisposedException>();

            act = () =>
            {
                context.HotplugRegisterCallback(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    (p1, p2, p3, p4) =>
                    {
                        return Enums.libusb_hotplug_return.REARM;
                    }
                );
            };
            act.Should().Throw<ObjectDisposedException>();

            act = () => context.HotplugDeregisterCallback(0);
            act.Should().Throw<ObjectDisposedException>();

            act = () => context.SetOption(0, 0);
            act.Should().Throw<ObjectDisposedException>();

            act = () => context.SetOption(0, 0);
            act.Should().Throw<ObjectDisposedException>();

            act = () => context.HandleEventsCompleted(0);
            act.Should().Throw<ObjectDisposedException>();
        });
    }
};
