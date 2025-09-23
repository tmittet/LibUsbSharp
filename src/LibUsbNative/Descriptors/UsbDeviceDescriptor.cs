using System.Text.Json.Serialization;
using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

public readonly record struct UsbDeviceDescriptor
{
    public byte BLength { get; }
    public libusb_descriptor_type BDescriptorType { get; }
    public ushort BcdUSB { get; }
    public libusb_class_code BDeviceClass { get; }
    public byte BDeviceSubClass { get; }
    public byte BDeviceProtocol { get; }
    public byte BMaxPacketSize0 { get; }
    public ushort IdVendor { get; }
    public ushort IdProduct { get; }
    public ushort BcdDevice { get; }
    public byte IManufacturer { get; }
    public byte IProduct { get; }
    public byte ISerialNumber { get; }
    public byte BNumConfigurations { get; }

    [JsonConstructor]
    public UsbDeviceDescriptor(
        byte bLength,
        libusb_descriptor_type bDescriptorType,
        ushort bcdUSB,
        libusb_class_code bDeviceClass,
        byte bDeviceSubClass,
        byte bDeviceProtocol,
        byte bMaxPacketSize0,
        ushort idVendor,
        ushort idProduct,
        ushort bcdDevice,
        byte iManufacturer,
        byte iProduct,
        byte iSerialNumber,
        byte bNumConfigurations
    )
    {
        BLength = bLength;
        BDescriptorType = bDescriptorType;
        BcdUSB = bcdUSB;
        BDeviceClass = bDeviceClass;
        BDeviceSubClass = bDeviceSubClass;
        BDeviceProtocol = bDeviceProtocol;
        BMaxPacketSize0 = bMaxPacketSize0;
        IdVendor = idVendor;
        IdProduct = idProduct;
        BcdDevice = bcdDevice;
        IManufacturer = iManufacturer;
        IProduct = iProduct;
        ISerialNumber = iSerialNumber;
        BNumConfigurations = bNumConfigurations;
    }
}
