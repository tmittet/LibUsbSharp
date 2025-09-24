using FluentAssertions;
using LibUsbNative.Enums;
using LibUsbNative.SafeHandles;
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

public abstract class Given_an_accessible_USB_device
{
    private readonly ITestOutputHelper output;
    private readonly List<string> stdout = [];
    private static readonly ReaderWriterLockSlim rw_lock = new();
    private readonly LibUsbNative libUsb;

    public Given_an_accessible_USB_device(ITestOutputHelper output, ILibUsbApi api)
    {
        this.output = output;
        libUsb = new LibUsbNative(api);

        var version = libUsb.GetVersion();
        output.WriteLine(version.ToString());
    }

    internal ISafeContext GetContext()
    {
        var context = libUsb.CreateContext();

        context.RegisterLogCallback(
            (level, message) =>
            {
                output.WriteLine($"[Libusb][{level}] {message}");
                stdout.Add(message);
            }
        );

        context.SetOption(libusb_option.LIBUSB_OPTION_LOG_LEVEL, 3);
        return context;
    }

    internal static void EnterReadLock(Action action)
    {
        rw_lock.EnterReadLock();
        try
        {
            action();
        }
        finally
        {
            rw_lock.ExitReadLock();
        }
    }

    internal static void EnterWriteLock(Action action)
    {
        rw_lock.EnterReadLock();
        try
        {
            action();
        }
        finally
        {
            rw_lock.ExitReadLock();
        }
    }

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
            _ = stdout.Should().NotContain(s => s.Contains("still referenced"));
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
            _ = stdout.Should().NotContain(s => s.Contains("still referenced"));
        });
    }
};
