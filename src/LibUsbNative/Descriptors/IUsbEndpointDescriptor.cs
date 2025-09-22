using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

public interface IUsbEndpointDescriptor
{
    byte BLength { get; }
    UsbDescriptorType BDescriptorType { get; }
    UsbEndpointAddress BEndpointAddress { get; }
    UsbEndpointAttributes BmAttributes { get; }
    ushort WMaxPacketSize { get; }
    byte BInterval { get; }
    byte BRefresh { get; }
    byte BSynchAddress { get; }
    byte[] Extra { get; }
}
