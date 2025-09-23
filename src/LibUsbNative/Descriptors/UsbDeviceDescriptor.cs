using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

public record class UsbDeviceDescriptor() : IUsbDeviceDescriptor
{
    public byte BLength { get; set; }
    public UsbDescriptorType BDescriptorType { get; set; }
    public ushort BcdUSB { get; set; }
    public UsbClass BDeviceClass { get; set; }
    public byte BDeviceSubClass { get; set; }
    public byte BDeviceProtocol { get; set; }
    public byte BMaxPacketSize0 { get; set; }
    public ushort IdVendor { get; set; }
    public ushort IdProduct { get; set; }
    public ushort BcdDevice { get; set; }
    public byte IManufacturer { get; set; }
    public byte IProduct { get; set; }
    public byte ISerialNumber { get; set; }
    public byte BNumConfigurations { get; set; }
}
