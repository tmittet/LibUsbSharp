namespace LibUsbSharp.Native.Tests.SafeHandles.SafeContext;

public class Given_an_accessible_USB_device_Fake(ITestOutputHelper output)
    : Given_an_accessible_USB_device(output, new FakeLibusbApi());

[Trait("Category", "UsbDevice")]
public class Given_an_accessible_USB_device_Real(ITestOutputHelper output)
    : Given_an_accessible_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_an_accessible_USB_device(ITestOutputHelper output, ILibUsbApi api)
    : LibUsbNativeTestBase(output, api)
{
    [SkippableFact]
    public void TestNoListOrHandleDispose()
    {
        EnterWriteLock(() =>
        {
            var context = GetContext();
            var list = context.GetDeviceList();
            var device = list.GetAccessibleDeviceOrSkipTest();

            var deviceHandle = device.Open();
            context.Dispose();
            _ = LibUsbOutput.Should().NotContain(s => s.Contains("still referenced"));
        });
    }

    [SkippableFact]
    public void TestDisposeInWrongOrder()
    {
        EnterWriteLock(() =>
        {
            var context = GetContext();
            var list = context.GetDeviceList();
            var device = list.GetAccessibleDeviceOrSkipTest();

            var deviceHandle = device.Open();

            context.Dispose();
            list.Dispose();

            deviceHandle.IsClosed.Should().BeFalse();
            deviceHandle.Dispose();
            _ = LibUsbOutput.Should().NotContain(s => s.Contains("still referenced"));
        });
    }
};
