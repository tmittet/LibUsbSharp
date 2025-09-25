namespace LibUsbNative.Tests.SafeHandles.SafeDeviceHandle;

[Trait("Category", "UsbDevice")]
public class Given_an_accessible_USB_device_Real(ITestOutputHelper output)
    : Given_an_accessible_USB_device(output, new PInvokeLibUsbApi());

public abstract class Given_an_accessible_USB_device(ITestOutputHelper output, ILibUsbApi api)
    : LibUsbNativeTestBase(output, api)
{
    [SkippableFact]
    public void TestOpenDeviceHandle()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            var device = list.GetAccessibleDeviceOrSkipTest();

            using var deviceHandle = device.Open();
            deviceHandle.IsClosed.Should().BeFalse();
        });
    }

    [SkippableFact]
    public void TestReadSerialNumber()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            var device = list.GetAccessibleDeviceOrSkipTest();

            using var deviceHandle = device.Open();
            deviceHandle.IsClosed.Should().BeFalse();
            var serialNumber = deviceHandle.GetStringDescriptorAscii(
                deviceHandle.Device.GetDeviceDescriptor().iSerialNumber
            );
            serialNumber.Should().NotBeNullOrEmpty();

            Output.WriteLine($"Serial Number: {serialNumber}");
        });
    }

    [SkippableFact]
    public void TryGetStringDescriptorAscii_successfully_returns_serial()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            var device = list.GetAccessibleDeviceOrSkipTest();

            using var deviceHandle = device.Open();
            var snIndex = device.GetDeviceDescriptor().iSerialNumber;
            var result = deviceHandle.TryGetStringDescriptorAscii(snIndex, out var value, out var error);
            result.Should().BeTrue();
            value.Should().NotBeNullOrEmpty();
            error.Should().BeNull();
            Output.WriteLine($"Serial Number: {value}");
        });
    }

    [SkippableFact]
    public void TestFailsAfterDispose()
    {
        EnterReadLock(() =>
        {
            using var context = GetContext();
            using var list = context.GetDeviceList();
            var device = list.GetAccessibleDeviceOrSkipTest();

            var deviceHandle = device.Open();
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
