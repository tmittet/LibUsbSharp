using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

public record class UsbEndpointDescriptor() : IUsbEndpointDescriptor
{
    public byte BLength { get; set; }
    public UsbDescriptorType BDescriptorType { get; set; }
    public UsbEndpointAddress BEndpointAddress { get; set; }
    public UsbEndpointAttributes BmAttributes { get; set; }
    public ushort WMaxPacketSize { get; set; }
    public byte BInterval { get; set; }
    public byte BRefresh { get; set; }
    public byte BSynchAddress { get; set; }
    public byte[] Extra { get; set; } = Array.Empty<byte>();
}
