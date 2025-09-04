using System;
using FluentAssertions;
using LibUsbNative;
using LibUsbNative.Extensions;
using LibUsbNative.SafeHandles;
using LibUsbNative.Tests.Fakes;
using Xunit;
using Xunit.Abstractions;

namespace LibUsbNative.Tests;

public class SafeDeviceHandleTests
{
    private readonly ITestOutputHelper output;
    private readonly ISafeContext context;
    private readonly List<string> stdout = [];
    private static readonly ReaderWriterLockSlim rw_lock = new();

    public SafeDeviceHandleTests(ITestOutputHelper output)
    {
        this.output = output;
        //Libusb.Api = new FakeLibusbApi();

        var version = LibUsb.GetVersion();
        output.WriteLine(version.ToString());

        context = LibUsb.CreateContext();

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
    public void TestOpenDeviceHandle()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var device = list.Devices.ToList()[1];
            var deviceHandle = device.Open();
            _ = deviceHandle.IsClosed.Should().BeFalse();

            list.Dispose();
            context.Dispose();
            deviceHandle.Dispose();
        });
    }

    [Fact]
    public void TestReadSerialNumber()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var device = list.Devices.ToList()[1];
            var deviceHandle = device.Open();
            _ = deviceHandle.IsClosed.Should().BeFalse();
            var serialNumber = deviceHandle.GetStringDescriptorAscii(
                deviceHandle.Device.GetDeviceDescriptor().ISerialNumber
            );
            _ = serialNumber.Should().NotBeNullOrEmpty();

            output.WriteLine($"Serial Number: {serialNumber}");

            list.Dispose();
            deviceHandle.Dispose();
            context.Dispose();
        });
    }
};
