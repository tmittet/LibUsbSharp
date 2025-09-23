using System.Text.Json.Serialization;
using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

/// <summary>
/// Combined representation of bEndpointAddress.
/// </summary>
public readonly record struct UsbEndpointAddress
{
    public UsbEndpointNumber Number { get; }
    public UsbEndpointDirection Direction { get; }
    public byte Raw { get; }

    [JsonConstructor]
    public UsbEndpointAddress(byte raw)
    {
        Raw = raw;
        Direction = (raw & 0x80) != 0 ? UsbEndpointDirection.In : UsbEndpointDirection.Out;
        Number = (UsbEndpointNumber)(raw & 0x0F);
    }

    public override string ToString() => $"{Direction} {Number} (0x{Raw:X2})";
}
