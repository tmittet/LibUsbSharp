using System.Text.Json;
using FluentAssertions;
using LibUsbNative.Enums;
using LibUsbNative.Extensions;
using LibUsbNative.SafeHandles;
using LibUsbNative.Structs;
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
    public void UsbConfigDescriptor_serializes_and_deserializes_successfully()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var device = list.Devices.ToList()[0];
            var json = device.GetActiveConfigDescriptor().ToJson();
            output.WriteLine(json);

            var deserialized = JsonSerializer.Deserialize<libusb_config_descriptor>(json)!;
            output.WriteLine(deserialized.ToJson());
            deserialized.ToJson().Should().Be(json);
            list.Dispose();
        });
    }

    [Fact]
    public void UsbDeviceDescriptor_serializes_and_deserializes_successfully()
    {
        EnterReadLock(() =>
        {
            var (list, count) = context.GetDeviceList();
            count.Should().BePositive();
            var device = list.Devices.ToList()[0];
            var json = device.GetDeviceDescriptor().ToJson();
            output.WriteLine(json);

            var deserialized = JsonSerializer.Deserialize<libusb_device_descriptor>(json)!;
            deserialized.ToJson().Should().Be(json);
            list.Dispose();
        });
    }
};
