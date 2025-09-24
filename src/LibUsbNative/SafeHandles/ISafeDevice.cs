using LibUsbNative.Structs;

namespace LibUsbNative.SafeHandles;

public interface ISafeDevice
{
    ISafeDeviceHandle Open();

    libusb_device_descriptor GetDeviceDescriptor();

    libusb_config_descriptor GetActiveConfigDescriptor();
    ISafeConfigDescriptorPtr GetActiveConfigDescriptorPtr();

    libusb_config_descriptor GetConfigDescriptor(byte configIndex);
    ISafeConfigDescriptorPtr GetConfigDescriptorPtr(byte configIndex);

    byte GetBusNumber();
    byte GetDeviceAddress();
    byte GetPortNumber();
}
