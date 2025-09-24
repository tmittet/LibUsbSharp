using FluentAssertions;
using LibUsbNative.Tests.Fakes;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.SafeHandles.SafeContext;

public class Given_an_accessible_USB_device_Fake : Given_an_accessible_USB_device
{
    public Given_an_accessible_USB_device_Fake(ITestOutputHelper output)
        : base(output, new FakeLibusbApi()) { }
}

public class Given_an_accessible_USB_device_Real : Given_an_accessible_USB_device
{
    public Given_an_accessible_USB_device_Real(ITestOutputHelper output)
        : base(output, new PInvokeLibUsbApi()) { }
}

public abstract class Given_an_accessible_USB_device(ITestOutputHelper output, ILibUsbApi api)
    : SafeHandlesTestBase(output, api)
{
    [Fact]
    public void TestNoListOrHandleDispose()
    {
        EnterWriteLock(() =>
        {
            var context = GetContext();
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            // TODO: Picks random device to open and fails. In some cases this results in:
            // Failed to open USB device. Operation not supported or unimplemented on this platform.
            var deviceHandle = list.Devices.ToList()[0].Open();
            context.Dispose();
            _ = LibUsbOutput.Should().NotContain(s => s.Contains("still referenced"));
        });
    }

    [Fact]
    public void TestDisposeInWrongOrder()
    {
        EnterWriteLock(() =>
        {
            var context = GetContext();
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            // TODO: Picks random device to open and fails. In some cases this results in:
            // Failed to open USB device. Operation not supported or unimplemented on this platform.
            var deviceHandle = list.Devices.ToList()[0].Open();

            context.Dispose();
            list.Dispose();

            deviceHandle.IsClosed.Should().BeFalse();
            deviceHandle.Dispose();
            _ = LibUsbOutput.Should().NotContain(s => s.Contains("still referenced"));
        });
    }
};
