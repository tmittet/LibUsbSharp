using System.Text.Json.Serialization;
using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

public readonly record struct UsbEndpointDescriptor
{
    public byte BLength { get; }
    public libusb_descriptor_type BDescriptorType { get; }
    public UsbEndpointAddress BEndpointAddress { get; }
    public UsbEndpointAttributes BmAttributes { get; }
    public ushort WMaxPacketSize { get; }
    public byte BInterval { get; }
    public byte BRefresh { get; }
    public byte BSynchAddress { get; }
    public byte[] Extra { get; } = Array.Empty<byte>();

    [JsonConstructor]
    public UsbEndpointDescriptor(
        byte bLength,
        libusb_descriptor_type bDescriptorType,
        UsbEndpointAddress bEndpointAddress,
        UsbEndpointAttributes bmAttributes,
        ushort wMaxPacketSize,
        byte bInterval,
        byte bRefresh,
        byte bSynchAddress,
        byte[] extra
    )
    {
        BLength = bLength;
        BDescriptorType = bDescriptorType;
        BEndpointAddress = bEndpointAddress;
        BmAttributes = bmAttributes;
        WMaxPacketSize = wMaxPacketSize;
        BInterval = bInterval;
        BRefresh = bRefresh;
        BSynchAddress = bSynchAddress;
        Extra = extra;
    }
}
