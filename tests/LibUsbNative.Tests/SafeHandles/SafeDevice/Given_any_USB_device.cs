using FluentAssertions;
using LibUsbNative.Enums;
using LibUsbNative.Tests.TestInfrastructure;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.SafeHandles.SafeDevice;

[Trait("Category", "UsbDevice")]
public class Given_any_USB_device_Real(ITestOutputHelper output) : Given_any_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_any_USB_device(ITestOutputHelper output, ILibUsbApi api) : LibUsbNativeTestBase(output, api)
{
    [SkippableFact]
    public void TestGetDeviceDescriptor()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            var device = list.GetAnyDeviceOrSkipTest();

            var descriptor = device.GetDeviceDescriptor();
            descriptor.bDescriptorType.Should().Be(libusb_descriptor_type.LIBUSB_DT_DEVICE);
        });
    }

    [SkippableFact]
    public void TestGetActiveConfigDescriptor()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            var device = list.GetAnyDeviceOrSkipTest();

            var descriptor = device.GetActiveConfigDescriptor();
            descriptor.bDescriptorType.Should().Be(libusb_descriptor_type.LIBUSB_DT_CONFIG);
        });
    }
};
