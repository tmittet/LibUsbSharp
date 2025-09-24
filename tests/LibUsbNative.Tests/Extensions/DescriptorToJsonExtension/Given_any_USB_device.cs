using System.Text.Json;
using FluentAssertions;
using LibUsbNative.Extensions;
using LibUsbNative.Structs;
using LibUsbNative.Tests.Fakes;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.Extensions.DescriptorToJsonExtension;

public class Given_any_USB_device_Fake(ITestOutputHelper output) : Given_any_USB_device(output, new FakeLibusbApi());

[Trait("Category", "UsbDevice")]
public class Given_any_USB_device_Real(ITestOutputHelper output) : Given_any_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_any_USB_device(ITestOutputHelper output, ILibUsbApi api) : LibUsbNativeTestBase(output, api)
{
    [Fact]
    public void UsbConfigDescriptor_serializes_and_deserializes_successfully()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            list.Count.Should().BePositive();
            var device = list[0];
            var json = device.GetActiveConfigDescriptor().ToJson();
            Output.WriteLine(json);

            var deserialized = JsonSerializer.Deserialize<libusb_config_descriptor>(json)!;
            deserialized.ToJson().Should().Be(json);
        });
    }

    [Fact]
    public void UsbDeviceDescriptor_serializes_and_deserializes_successfully()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            list.Count.Should().BePositive();
            var device = list[0];
            var json = device.GetDeviceDescriptor().ToJson();
            Output.WriteLine(json);

            var deserialized = JsonSerializer.Deserialize<libusb_device_descriptor>(json)!;
            deserialized.ToJson().Should().Be(json);
        });
    }
};
