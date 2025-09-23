using System.Text.Json.Serialization;
using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

public readonly record struct UsbConfigDescriptor
{
    public byte BLength { get; }
    public libusb_descriptor_type BDescriptorType { get; }
    public ushort WTotalLength { get; }
    public byte BNumInterfaces { get; }
    public byte BConfigurationValue { get; }
    public byte IConfiguration { get; }
    public UsbConfigAttributes BmAttributes { get; }
    public byte MaxPower { get; }
    public IReadOnlyList<UsbInterface> Interfaces { get; } = Array.Empty<UsbInterface>();
    public byte[] Extra { get; } = Array.Empty<byte>();

    [JsonConstructor]
    public UsbConfigDescriptor(
        byte bLength,
        libusb_descriptor_type bDescriptorType,
        ushort wTotalLength,
        byte bNumInterfaces,
        byte bConfigurationValue,
        byte iConfiguration,
        UsbConfigAttributes bmAttributes,
        byte maxPower,
        IReadOnlyList<UsbInterface> interfaces,
        byte[] extra
    )
    {
        BLength = bLength;
        BDescriptorType = bDescriptorType;
        WTotalLength = wTotalLength;
        BNumInterfaces = bNumInterfaces;
        BConfigurationValue = bConfigurationValue;
        IConfiguration = iConfiguration;
        BmAttributes = bmAttributes;
        MaxPower = maxPower;
        Interfaces = interfaces;
        Extra = extra;
    }
}
