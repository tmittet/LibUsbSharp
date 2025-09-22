using System.Text.Json;
using FluentAssertions;
using LibUsbNative.Descriptor;
using LibUsbNative.Descriptors;
using LibUsbNative.SafeHandles;
using Xunit.Abstractions;

namespace LibUsbNative.Tests;

public class DescriptorExtensionsTests
{
    private readonly ITestOutputHelper output;
    private readonly ISafeContext context;
    private readonly List<string> stdout = new();
    private static readonly ReaderWriterLockSlim rw_lock = new();
    private readonly LibUsbNative libUsb;

    public DescriptorExtensionsTests(ITestOutputHelper output)
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

            UsbDeviceDescriptor deserialized = JsonSerializer.Deserialize<UsbDeviceDescriptor>(json)!;
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

            UsbConfigDescriptor deserialized = JsonSerializer.Deserialize<UsbConfigDescriptor>(json)!;
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
