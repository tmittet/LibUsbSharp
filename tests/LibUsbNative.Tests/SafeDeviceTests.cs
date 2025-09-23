using FluentAssertions;
using LibUsbNative.Enums;
using LibUsbNative.SafeHandles;
using Xunit.Abstractions;

namespace LibUsbNative.Tests;

public class SafeDeviceTests
{
    private readonly ITestOutputHelper output;
    private readonly ISafeContext context;
    private readonly List<string> stdout = new();
    private static readonly ReaderWriterLockSlim rw_lock = new();
    private readonly LibUsbNative libUsb;

    public SafeDeviceTests(ITestOutputHelper output)
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

        context.SetOption(LibUsbOption.LOG_LEVEL, 3);
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
    public void TestGetDeviceDescriptor()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();

            var device = list.Devices.ToList()[0];
            var descriptor = device.GetDeviceDescriptor();
            descriptor.BDescriptorType.Should().Be(UsbDescriptorType.Device);

            list.Dispose();
        });
    }

    [Fact]
    public void TestGetActiveConfigDescriptor()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();

            var device = list.Devices.ToList()[0];
            var descriptor = device.GetActiveConfigDescriptor();
            descriptor.BDescriptorType.Should().Be(UsbDescriptorType.Configuration);

            list.Dispose();
        });
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
