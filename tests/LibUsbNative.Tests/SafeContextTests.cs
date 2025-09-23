using FluentAssertions;
using LibUsbNative.Enums;
using LibUsbNative.SafeHandles;
using LibUsbNative.Tests.Fakes;
using Xunit.Abstractions;

namespace LibUsbNative.Tests;

public class SafeContextTests_Fake : SafeContextTests
{
    public SafeContextTests_Fake(ITestOutputHelper output)
        : base(output, new FakeLibusbApi()) { }
}

public class SafeContextTests_Real : SafeContextTests
{
    public SafeContextTests_Real(ITestOutputHelper output)
        : base(output, new PInvokeLibUsbApi()) { }
}

public abstract class SafeContextTests
{
    private readonly ITestOutputHelper output;
    private readonly List<string> stdout = new();
    private static readonly ReaderWriterLockSlim rw_lock = new();
    private readonly LibUsbNative libUsb;

    public SafeContextTests(ITestOutputHelper output, ILibUsbApi api)
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
    public void TestTwoContexts()
    {
        EnterReadLock(() =>
        {
            var context = GetContext();
            var context2 = GetContext();

            var (list, count) = context.GetDeviceList();
            var (list2, count2) = context2.GetDeviceList();

            count.Should().BePositive();
            count2.Equals(count);

            context.Dispose();
            context2.Dispose();

            _ = stdout.Should().NotContain(s => s.Contains("still referenced"));
        });
    }

    [Fact]
    public void TestGetDeviceList()
    {
        EnterReadLock(() =>
        {
            var context = GetContext();
            var (list, count) = context.GetDeviceList();

            count.Should().BePositive();
            list.Devices.ToList().Should().HaveCount((int)count);

            list.Dispose();
            context.Dispose();

            _ = stdout.Should().NotContain(s => s.Contains("still referenced"));
        });
    }

    [Fact]
    public void TestNoListDispose()
    {
        EnterReadLock(() =>
        {
            var context = GetContext();
            var (list, count) = context.GetDeviceList();
            context.Dispose();
            _ = stdout.Should().NotContain(s => s.Contains("still referenced"));
        });
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
                    (IntPtr)0,
                    (p1, p2, p3, p4) =>
                    {
                        return true;
                    }
                );
            };
            act.Should().Throw<ObjectDisposedException>();

            act = () => context.HotplugDeregisterCallback((IntPtr)0);
            act.Should().Throw<ObjectDisposedException>();

            act = () => context.SetOption(0, 0);
            act.Should().Throw<ObjectDisposedException>();

            act = () => context.SetOption((libusb_option)0, 0);
            act.Should().Throw<ObjectDisposedException>();

            act = () => context.HandleEventsCompleted((IntPtr)0);
            act.Should().Throw<ObjectDisposedException>();
        });
    }
};
