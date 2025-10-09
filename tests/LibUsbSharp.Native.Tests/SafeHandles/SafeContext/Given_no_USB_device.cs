namespace LibUsbSharp.Native.Tests.SafeHandles.SafeContext;

public class Given_no_USB_device_Fake(ITestOutputHelper output) : Given_no_USB_device(output, new FakeLibusbApi());

public class Given_no_USB_device_Real(ITestOutputHelper output) : Given_no_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_no_USB_device(ITestOutputHelper output, ILibUsbApi api) : LibUsbNativeTestBase(output, api)
{
    [Fact]
    public void Disposing_SafeContext_with_open_SafeDeviceList_blocks_context_ReleaseHandle()
    {
        var context = (Native.SafeHandles.SafeContext)GetContext();
        var list = context.GetDeviceList();
        context.Dispose();

        // SafeContext handle will not be closed until after SafeDeviceList is disposed
        context.IsClosed.Should().BeFalse();
        _ = LibUsbOutput.Should().NotContain(s => s.Contains("still referenced"));

        list.Dispose();
    }

    [Fact]
    public void SafeContext_does_ReleaseHandle_when_open_SafeDeviceList_is_disposed()
    {
        var context = (Native.SafeHandles.SafeContext)GetContext();
        var list = context.GetDeviceList();
        context.Dispose();
        list.Dispose();

        // SafeContext handle should be closed when SafeDeviceList is disposed
        context.IsClosed.Should().BeTrue();
        _ = LibUsbOutput.Should().NotContain(s => s.Contains("still referenced"));
    }

    [Fact]
    public void GetDeviceList_throws_ObjectDisposedException_after_SafeContext_Dispose()
    {
        var context = GetContext();
        context.Dispose();
        Action act = () => context.GetDeviceList();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void RegisterLogCallback_is_successful_when_called_once()
    {
        using var context = GetContext(Enums.libusb_log_level.LIBUSB_LOG_LEVEL_NONE);
        context.RegisterLogCallback((_, message) => Output.WriteLine(message));
    }

    [Fact]
    public void RegisterLogCallback_throws_InvalidOperationException_when_called_more_than_once()
    {
        using var context = GetContext(Enums.libusb_log_level.LIBUSB_LOG_LEVEL_NONE);
        context.RegisterLogCallback((_, message) => { });
        var act = () =>
        {
            context.RegisterLogCallback((level, message) => { });
        };
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RegisterLogCallback_throws_ObjectDisposedException_after_SafeContext_Dispose()
    {
        var context = GetContext();
        context.Dispose();
        var act = () =>
        {
            context.RegisterLogCallback((level, message) => { });
        };
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void HotplugRegisterCallback_throws_ObjectDisposedException_after_SafeContext_Dispose()
    {
        var context = GetContext();
        context.Dispose();
        var act = () =>
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
    }

    [Fact]
    public void SetOption_int_throws_ObjectDisposedException_after_SafeContext_Dispose()
    {
        var context = GetContext();
        context.Dispose();
        var act = () => context.SetOption(0, 0);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void SetOption_IntPtr_throws_ObjectDisposedException_after_SafeContext_Dispose()
    {
        var context = GetContext();
        context.Dispose();
        var act = () => context.SetOption(0, IntPtr.Zero);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void HandleEventsCompleted_throws_ObjectDisposedException_after_SafeContext_Dispose()
    {
        var context = GetContext();
        context.Dispose();
        var act = () => context.HandleEventsCompleted(0);
        act.Should().Throw<ObjectDisposedException>();
    }
};
