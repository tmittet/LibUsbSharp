using LibUsbNative.Descriptors;

namespace LibUsbNative.SafeHandles;

public interface ISafeDevice
{
    ISafeDeviceHandle Open();

    UsbDeviceDescriptor GetDeviceDescriptor();

    UsbConfigDescriptor GetActiveConfigDescriptor();
    ISafeConfigDescriptorPtr GetActiveConfigDescriptorPtr();

    UsbConfigDescriptor GetConfigDescriptor(byte configIndex);
    ISafeConfigDescriptorPtr GetConfigDescriptorPtr(byte configIndex);

    byte GetBusNumber();
    byte GetDeviceAddress();
    byte GetPortNumber();
}
