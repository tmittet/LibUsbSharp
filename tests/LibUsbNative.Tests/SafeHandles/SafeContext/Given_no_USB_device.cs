using FluentAssertions;
using LibUsbNative.Tests.Fakes;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.SafeHandles.SafeContext;

public class Given_no_USB_device_Fake(ITestOutputHelper output) : Given_no_USB_device(output, new FakeLibusbApi());

public class Given_no_USB_device_Real(ITestOutputHelper output) : Given_no_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_no_USB_device(ITestOutputHelper output, ILibUsbApi api) : LibUsbNativeTestBase(output, api)
{
    [Fact]
    public void TestTwoContexts()
    {
        EnterReadLock(() =>
        {
            var context = GetContext();
            var context2 = GetContext();

            var list = context.GetDeviceList();
            var list2 = context2.GetDeviceList();

            list.Count.Should().BePositive();
            list2.Count.Equals(list.Count);

            context.Dispose();
            context2.Dispose();

            _ = LibUsbOutput.Should().NotContain(s => s.Contains("still referenced"));
        });
    }

    [Fact]
    public void TestGetDeviceList()
    {
        EnterReadLock(() =>
        {
            var context = GetContext();
            var list = context.GetDeviceList();

            list.Count.Should().BePositive();
            list.Should().HaveCount(list.Count);

            list.Dispose();
            context.Dispose();

            _ = LibUsbOutput.Should().NotContain(s => s.Contains("still referenced"));
        });
    }

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
                        return true;
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
