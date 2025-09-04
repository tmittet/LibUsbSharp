using System;
using FluentAssertions;
using LibUsbNative;
using LibUsbNative.Extensions;
using LibUsbNative.SafeHandles;
using LibUsbNative.Tests.Fakes;
using Xunit;
using Xunit.Abstractions;

namespace LibUsbNative.Tests;

public class SafeDeviceTests
{
    private readonly ITestOutputHelper output;
    private readonly ISafeContext context;
    private readonly List<string> stdout = [];
    private static readonly ReaderWriterLockSlim rw_lock = new();

    public SafeDeviceTests(ITestOutputHelper output)
    {
        this.output = output;
        LibUsbNative.Api = new FakeLibusbApi();

        var version = LibUsbNative.GetVersion();
        output.WriteLine(version.ToString());

        context = LibUsbNative.CreateContext();

        context.RegisterLogCallback(
            (level, message) =>
            {
                output.WriteLine($"[Libusb][{level}] {message}");
                stdout.Add(message);
            }
        );

        context.SetOption(LibusbOption.LOG_LEVEL, 3);
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
