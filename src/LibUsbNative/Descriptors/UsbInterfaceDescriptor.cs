using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

// TODO: Fix this
#pragma warning disable SYSLIB1037

public record UsbInterfaceDescriptor(
    byte BLength,
    UsbDescriptorType BDescriptorType,
    byte BInterfaceNumber,
    byte BAlternateSetting,
    byte BNumEndpoints,
    UsbClass BInterfaceClass,
    byte BInterfaceSubClass,
    byte BInterfaceProtocol,
    byte IInterface,
    UsbEndpointDescriptor[] Endpoints,
    byte[] Extra
) : IUsbInterfaceDescriptor
{
    IReadOnlyList<IUsbEndpointDescriptor> IUsbInterfaceDescriptor.Endpoints => Array.AsReadOnly(Endpoints);
}

#pragma warning restore SYSLIB1037
