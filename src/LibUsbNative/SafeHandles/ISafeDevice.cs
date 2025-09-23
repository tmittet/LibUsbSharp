using LibUsbNative.Descriptors;

namespace LibUsbNative.SafeHandles;

public interface ISafeDevice
{
    ISafeDeviceHandle Open();

    IUsbDeviceDescriptor GetDeviceDescriptor();

    IUsbConfigDescriptor GetActiveConfigDescriptor();
    ISafeConfigDescriptorPtr GetActiveConfigDescriptorPtr();

    IUsbConfigDescriptor GetConfigDescriptor(byte configIndex);
    ISafeConfigDescriptorPtr GetConfigDescriptorPtr(byte configIndex);

    byte GetBusNumber();
    byte GetDeviceAddress();
    byte GetPortNumber();
}
