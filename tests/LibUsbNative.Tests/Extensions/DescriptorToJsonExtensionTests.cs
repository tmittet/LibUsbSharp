using System.Text.Json;
using FluentAssertions;
using LibUsbNative.Descriptors;
using LibUsbNative.Extensions;
using LibUsbNative.SafeHandles;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.Extensions;

public class DescriptorToJsonExtensionTests
{
    private readonly ITestOutputHelper output;
    private readonly ISafeContext context;
    private readonly List<string> stdout = [];
    private static readonly ReaderWriterLockSlim rw_lock = new();
    private readonly LibUsbNative libUsb;

    public DescriptorToJsonExtensionTests(ITestOutputHelper output)
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
    public void TestDeviceDescriptorToJsonRaw()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var device = list.Devices.ToList()[0];
            var json = device.GetDeviceDescriptor().ToJson(raw: true);
            output.WriteLine(json);

            var deserialized = JsonSerializer.Deserialize<UsbDeviceDescriptor>(json)!;
            deserialized.ToJson(raw: true).Should().Be(json);

            list.Dispose();
        });
    }

    [Fact]
    public void TestActiveConfigToJsonRaw()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var device = list.Devices.ToList()[0];
            var json = device.GetActiveConfigDescriptor().ToJson(raw: true);
            output.WriteLine("orginal:");
            output.WriteLine(json);

            var deserialized = JsonSerializer.Deserialize<UsbConfigDescriptor>(json)!;
            output.WriteLine("new:");
            output.WriteLine(deserialized.ToJson(raw: true));

            //deserialized.ToJson(raw: true).Should().Be(json);

            list.Dispose();
        });
    }

    [Fact]
    public void TestDeviceDescriptorToJsonFancy()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var device = list.Devices.ToList()[0];
            var json = device.GetDeviceDescriptor().ToJson();
            output.WriteLine(json);

            list.Dispose();
        });
    }

    [Fact]
    public void TestActiveConfigToJsonFancy()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var device = list.Devices.ToList()[0];
            var json = device.GetActiveConfigDescriptor().ToJson();
            output.WriteLine(json);

            list.Dispose();
        });
    }
};
