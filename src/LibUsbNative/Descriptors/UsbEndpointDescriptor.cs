using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

// TODO: Fix this
#pragma warning disable SYSLIB1037

public record UsbEndpointDescriptor(
    byte BLength,
    UsbDescriptorType BDescriptorType,
    UsbEndpointAddress BEndpointAddress,
    UsbEndpointAttributes BmAttributes,
    ushort WMaxPacketSize,
    byte BInterval,
    byte BRefresh,
    byte BSynchAddress,
    byte[] Extra
) : IUsbEndpointDescriptor;

#pragma warning restore SYSLIB1037
