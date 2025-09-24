using FluentAssertions;
using Xunit.Abstractions;

namespace LibUsbNative.Tests.SafeHandles.SafeDeviceHandle;

[Trait("Category", "UsbDevice")]
public class Given_an_accessible_USB_device_Real(ITestOutputHelper output)
    : Given_an_accessible_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_an_accessible_USB_device(ITestOutputHelper output, ILibUsbApi api)
    : LibUsbNativeTestBase(output, api)
{
    [Fact]
    public void TestOpenDeviceHandle()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            list.Count.Should().BePositive();
            var device = list[0];
            // TODO: Picks random device to open and fails. In some cases this results in:
            // Failed to open USB device. Operation not supported or unimplemented on this platform.
            using var deviceHandle = device.Open();
            _ = deviceHandle.IsClosed.Should().BeFalse();
        });
    }

    [Fact]
    public void TestReadSerialNumber()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            list.Count.Should().BePositive();
            var device = list[0];
            // TODO: Picks random device to open and fails. In some cases this results in:
            // Failed to open USB device. Operation not supported or unimplemented on this platform.
            using var deviceHandle = device.Open();
            _ = deviceHandle.IsClosed.Should().BeFalse();
            var serialNumber = deviceHandle.GetStringDescriptorAscii(
                deviceHandle.Device.GetDeviceDescriptor().iSerialNumber
            );
            _ = serialNumber.Should().NotBeNullOrEmpty();

            Output.WriteLine($"Serial Number: {serialNumber}");
        });
    }

    [Fact]
    public void TestFailsAfterDispose()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            list.Count.Should().BePositive();

            // TODO: Picks random device to open and fails. In some cases this results in:
            // Failed to open USB device. Operation not supported or unimplemented on this platform.
            var deviceHandle = list[0].Open();
            deviceHandle.Dispose();

            Action act = () => deviceHandle.ClaimInterface(1);
            act.Should().Throw<ObjectDisposedException>();

            act = () =>
            {
                var d = deviceHandle.Device;
            };
            act.Should().Throw<ObjectDisposedException>();

            act = () => deviceHandle.GetStringDescriptorAscii(1);
            act.Should().Throw<ObjectDisposedException>();

            act = () => deviceHandle.ResetDevice();
            act.Should().Throw<ObjectDisposedException>();
        });
    }
};
