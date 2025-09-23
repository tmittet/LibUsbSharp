using System.Text.Json.Serialization;
using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

public readonly record struct UsbInterfaceDescriptor
{
    public byte BLength { get; }
    public UsbDescriptorType BDescriptorType { get; }
    public byte BInterfaceNumber { get; }
    public byte BAlternateSetting { get; }
    public byte BNumEndpoints { get; }
    public libusb_class_code BInterfaceClass { get; }
    public byte BInterfaceSubClass { get; }
    public byte BInterfaceProtocol { get; }
    public byte IInterface { get; }
    public IReadOnlyList<UsbEndpointDescriptor> Endpoints { get; } = Array.Empty<UsbEndpointDescriptor>();
    public byte[] Extra { get; } = Array.Empty<byte>();

    [JsonConstructor]
    public UsbInterfaceDescriptor(
        byte bLength,
        UsbDescriptorType bDescriptorType,
        byte bInterfaceNumber,
        byte bAlternateSetting,
        byte bNumEndpoints,
        libusb_class_code bInterfaceClass,
        byte bInterfaceSubClass,
        byte bInterfaceProtocol,
        byte iInterface,
        IReadOnlyList<UsbEndpointDescriptor> endpoints,
        byte[] extra
    )
    {
        BLength = bLength;
        BDescriptorType = bDescriptorType;
        BInterfaceNumber = bInterfaceNumber;
        BAlternateSetting = bAlternateSetting;
        BNumEndpoints = bNumEndpoints;
        BInterfaceClass = bInterfaceClass;
        BInterfaceSubClass = bInterfaceSubClass;
        BInterfaceProtocol = bInterfaceProtocol;
        IInterface = iInterface;
        Endpoints = endpoints;
        Extra = extra;
    }
}
