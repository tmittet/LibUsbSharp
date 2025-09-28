namespace LibUsbSharp.Native.Tests.SafeHandles.SafeContext;

public class Given_any_USB_device_Fake(ITestOutputHelper output) : Given_any_USB_device(output, new FakeLibusbApi());

[Trait("Category", "UsbDevice")]
public class Given_any_USB_device_Real(ITestOutputHelper output) : Given_any_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_any_USB_device(ITestOutputHelper output, ILibUsbApi api) : LibUsbNativeTestBase(output, api)
{
    [SkippableFact]
    public void TestTwoContexts()
    {
        EnterReadLock(() =>
        {
            var context = GetContext();
            var context2 = GetContext();

            var list = context.GetDeviceList();
            _ = list.GetAnyDeviceOrSkipTest();

            var list2 = context2.GetDeviceList();
            list.Count.Should().BePositive();
            list2.Count.Equals(list.Count);

            context.Dispose();
            context2.Dispose();

            _ = LibUsbOutput.Should().NotContain(s => s.Contains("still referenced"));
        });
    }

    [SkippableFact]
    public void TestGetDeviceList()
    {
        EnterReadLock(() =>
        {
            var context = GetContext();
            var list = context.GetDeviceList();
            _ = list.GetAnyDeviceOrSkipTest();

            list.Count.Should().BePositive();
            list.Should().HaveCount(list.Count);

            list.Dispose();
            context.Dispose();

            _ = LibUsbOutput.Should().NotContain(s => s.Contains("still referenced"));
        });
    }
};
