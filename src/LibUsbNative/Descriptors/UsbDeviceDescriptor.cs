using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

// TODO: Fix this
#pragma warning disable SYSLIB1037

public record UsbDeviceDescriptor(
    byte BLength,
    UsbDescriptorType BDescriptorType,
    ushort BcdUSB,
    UsbClass BDeviceClass,
    byte BDeviceSubClass,
    byte BDeviceProtocol,
    byte BMaxPacketSize0,
    ushort IdVendor,
    ushort IdProduct,
    ushort BcdDevice,
    byte IManufacturer,
    byte IProduct,
    byte ISerialNumber,
    byte BNumConfigurations
) : IUsbDeviceDescriptor;

#pragma warning restore SYSLIB1037
