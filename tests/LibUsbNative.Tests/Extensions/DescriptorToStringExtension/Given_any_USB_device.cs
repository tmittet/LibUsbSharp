using FluentAssertions;
using LibUsbNative.Extensions;
using LibUsbNative.Tests.Fakes;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.Extensions.DescriptorToStringExtension;

public class Given_any_USB_device_Fake(ITestOutputHelper output) : Given_any_USB_device(output, new FakeLibusbApi());

[Trait("Category", "UsbDevice")]
public class Given_any_USB_device_Real(ITestOutputHelper output) : Given_any_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_any_USB_device(ITestOutputHelper output, ILibUsbApi api) : LibUsbNativeTestBase(output, api)
{
    [Fact]
    public void TestDeviceDescriptorTreeString()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            list.Count.Should().BePositive();
            var device = list[0];
            var descriptor = device.GetDeviceDescriptor();
            Output.WriteLine(descriptor.ToTreeString());
        });
    }

    [Fact]
    public void TestActiveConfigTreeString()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            list.Count.Should().BePositive();
            var device = list[0];
            var descriptor = device.GetActiveConfigDescriptor();
            Output.WriteLine(descriptor.ToTreeString());
        });
    }
};
