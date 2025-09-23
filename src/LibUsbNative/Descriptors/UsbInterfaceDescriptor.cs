using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

public record class UsbInterfaceDescriptor() : IUsbInterfaceDescriptor
{
    public byte BLength { get; set; }
    public UsbDescriptorType BDescriptorType { get; set; }
    public byte BInterfaceNumber { get; set; }
    public byte BAlternateSetting { get; set; }
    public byte BNumEndpoints { get; set; }
    public UsbClass BInterfaceClass { get; set; }
    public byte BInterfaceSubClass { get; set; }
    public byte BInterfaceProtocol { get; set; }
    public byte IInterface { get; set; }
    public IReadOnlyList<IUsbEndpointDescriptor> Endpoints { get; set; } = Array.Empty<IUsbEndpointDescriptor>();
    public byte[] Extra { get; set; } = Array.Empty<byte>();
}
