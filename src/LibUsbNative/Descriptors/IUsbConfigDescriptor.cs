using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

public interface IUsbConfigDescriptor
{
    byte BLength { get; }
    UsbDescriptorType BDescriptorType { get; }
    ushort WTotalLength { get; }
    byte BNumInterfaces { get; }
    byte BConfigurationValue { get; }
    byte IConfiguration { get; }
    UsbConfigAttributes BmAttributes { get; }
    byte MaxPower { get; }
    IReadOnlyList<IUsbInterface> Interfaces { get; }
    byte[] Extra { get; }
}
