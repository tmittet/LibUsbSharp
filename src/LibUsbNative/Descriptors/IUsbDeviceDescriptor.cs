using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

// --------------------------------------------------------------------
// Public interfaces
// --------------------------------------------------------------------
public interface IUsbDeviceDescriptor
{
    byte BLength { get; }
    UsbDescriptorType BDescriptorType { get; }
    ushort BcdUSB { get; }
    UsbClass BDeviceClass { get; }
    byte BDeviceSubClass { get; }
    byte BDeviceProtocol { get; }
    byte BMaxPacketSize0 { get; }
    ushort IdVendor { get; }
    ushort IdProduct { get; }
    ushort BcdDevice { get; }
    byte IManufacturer { get; }
    byte IProduct { get; }
    byte ISerialNumber { get; }
    byte BNumConfigurations { get; }
}
