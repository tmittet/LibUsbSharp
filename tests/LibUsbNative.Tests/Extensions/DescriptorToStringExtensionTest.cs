using FluentAssertions;
using LibUsbNative.Enums;
using LibUsbNative.Extensions;
using LibUsbNative.SafeHandles;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.Extensions;

public class DescriptorToStringExtensionTest
{
    private readonly ITestOutputHelper output;
    private readonly ISafeContext context;
    private readonly List<string> stdout = new();
    private static readonly ReaderWriterLockSlim rw_lock = new();
    private readonly LibUsbNative libUsb;

    public DescriptorToStringExtensionTest(ITestOutputHelper output)
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

        context.SetOption(libusb_option.LIBUSB_OPTION_LOG_LEVEL, 3);
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
    public void TestDeviceDescriptorTreeString()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var device = list.Devices.ToList()[0];
            var descriptor = device.GetDeviceDescriptor();
            output.WriteLine(descriptor.ToTreeString());

            list.Dispose();
        });
    }

    [Fact]
    public void TestActiveConfigTreeString()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var device = list.Devices.ToList()[0];
            var descriptor = device.GetActiveConfigDescriptor();
            output.WriteLine(descriptor.ToTreeString());

            list.Dispose();
        });
    }
};
