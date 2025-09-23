using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

public record class UsbConfigDescriptor : IUsbConfigDescriptor
{
    public byte BLength { get; set; }
    public UsbDescriptorType BDescriptorType { get; set; }
    public ushort WTotalLength { get; set; }
    public byte BNumInterfaces { get; set; }
    public byte BConfigurationValue { get; set; }
    public byte IConfiguration { get; set; }
    public UsbConfigAttributes BmAttributes { get; set; }
    public byte MaxPower { get; set; }
    public IReadOnlyList<IUsbInterface> Interfaces { get; set; } = Array.Empty<IUsbInterface>();
    public byte[] Extra { get; set; } = Array.Empty<byte>();
}
