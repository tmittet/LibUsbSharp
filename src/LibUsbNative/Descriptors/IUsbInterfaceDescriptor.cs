using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

public interface IUsbInterfaceDescriptor
{
    byte BLength { get; }
    UsbDescriptorType BDescriptorType { get; }
    byte BInterfaceNumber { get; }
    byte BAlternateSetting { get; }
    byte BNumEndpoints { get; }
    UsbClass BInterfaceClass { get; }
    byte BInterfaceSubClass { get; }
    byte BInterfaceProtocol { get; }
    byte IInterface { get; }
    IReadOnlyList<IUsbEndpointDescriptor> Endpoints { get; }
    byte[] Extra { get; }
}
