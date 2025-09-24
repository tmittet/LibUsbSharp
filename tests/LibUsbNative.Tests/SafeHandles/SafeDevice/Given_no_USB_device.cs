using FluentAssertions;
using LibUsbNative.Enums;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.SafeHandles.SafeDevice;

public class Given_no_USB_device_Real(ITestOutputHelper output) : Given_no_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_no_USB_device(ITestOutputHelper output, ILibUsbApi api) : LibUsbNativeTestBase(output, api)
{
    [Fact]
    public void TestGetDeviceDescriptor()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            list.Count.Should().BePositive();

            var device = list[0];
            var descriptor = device.GetDeviceDescriptor();
            descriptor.bDescriptorType.Should().Be(libusb_descriptor_type.LIBUSB_DT_DEVICE);
        });
    }

    [Fact]
    public void TestGetActiveConfigDescriptor()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            list.Count.Should().BePositive();

            var device = list[0];
            var descriptor = device.GetActiveConfigDescriptor();
            descriptor.bDescriptorType.Should().Be(libusb_descriptor_type.LIBUSB_DT_CONFIG);
        });
    }
};
