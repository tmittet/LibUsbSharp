using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

// TODO: Fix this
#pragma warning disable SYSLIB1037

public record UsbConfigDescriptor(
    byte BLength,
    UsbDescriptorType BDescriptorType,
    ushort WTotalLength,
    byte BNumInterfaces,
    byte BConfigurationValue,
    byte IConfiguration,
    UsbConfigAttributes BmAttributes,
    byte MaxPower,
    UsbInterface[] Interfaces,
    byte[] Extra
) : IUsbConfigDescriptor
{
    IReadOnlyList<IUsbInterface> IUsbConfigDescriptor.Interfaces => Array.AsReadOnly(Interfaces);
}

#pragma warning restore SYSLIB1037
