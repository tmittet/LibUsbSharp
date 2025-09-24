using LibUsbNative.Descriptors;

namespace LibUsbNative.SafeHandles;

public interface ISafeDevice
{
    ISafeDeviceHandle Open();

    UsbDeviceDescriptor GetDeviceDescriptor();

    libusb_config_descriptor GetActiveConfigDescriptor();
    ISafeConfigDescriptorPtr GetActiveConfigDescriptorPtr();

    libusb_config_descriptor GetConfigDescriptor(byte configIndex);
    ISafeConfigDescriptorPtr GetConfigDescriptorPtr(byte configIndex);

    byte GetBusNumber();
    byte GetDeviceAddress();
    byte GetPortNumber();
}
