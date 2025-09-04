using System;
using FluentAssertions;
using LibUsbNative;
using LibUsbNative.Extensions;
using LibUsbNative.SafeHandles;
using LibUsbNative.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LibUsbNative.Tests;

public class SafeHandleTests
{
    private readonly ITestOutputHelper output;
    private readonly ISafeContext context;
    private readonly List<string> stdout = [];
    private static readonly ReaderWriterLockSlim rw_lock = new();

    public SafeHandleTests(ITestOutputHelper output)
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
    public void Test1()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            list.Devices.ToList().Should().HaveCount((int)count);
            context.Dispose();
            _ = stdout.Where(s => s.Contains("still referenced")).Should().HaveCount((int)count);
        });
    }

    [Fact]
    public void Test11()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            list.Devices.ToList().Should().HaveCount((int)count);
            context.SetOption(LibusbOption.LOG_LEVEL, 0);
            context.Dispose();
            _ = stdout.Should().NotContain(s => s.Contains("still referenced"));
        });
    }

    [Fact]
    public void Test2()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            list.Dispose();
            context.Dispose();
            _ = stdout.Should().NotContain(s => s.Contains("still referenced"));
        });
    }

    [Fact]
    public void Test3()
    {
        EnterWriteLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var deviceHandle = list.Devices.ToList()[1].Open();
            deviceHandle.Dispose();
            list.Dispose();
            context.Dispose();
            _ = stdout.Should().NotContain(s => s.Contains("still referenced"));
        });
    }

    [Fact]
    public void Test4()
    {
        EnterWriteLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var deviceHandle = list.Devices.ToList()[1].Open();
            //deviceHandle.Dispose();
            try
            {
                list.Dispose();
                context.Dispose();
                _ = stdout.Where(s => s.Contains("still referenced")).Should().HaveCount(2);
            }
            finally
            {
                deviceHandle.IsClosed.Should().BeFalse();
                deviceHandle.Dispose();
            }
        });
    }

    [Fact]
    public void Test5()
    {
        EnterWriteLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var deviceHandle = list.Devices.ToList()[1].Open();

            try
            {
                context.Dispose();
                _ = stdout.Where(s => s.Contains("still referenced")).Should().HaveCount((int)count);
            }
            finally
            {
                list.IsClosed.Should().BeFalse();
                list.Dispose();

                deviceHandle.IsClosed.Should().BeFalse();

                var act = () => deviceHandle.Dispose();
                act.Should().Throw<AccessViolationException>();
            }
        });
    }
};
