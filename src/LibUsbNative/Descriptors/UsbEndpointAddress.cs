using System.Text.Json.Serialization;
using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

/// <summary>
/// Combined representation of bEndpointAddress.
/// </summary>
public readonly record struct UsbEndpointAddress
{
    /// <summary>
    /// Bits 0:3 of the raw value are the endpoint number.
    /// </summary>
    public libusb_endpoint_number Number { get; }

    /// <summary>
    /// Bit 7 of the raw value indicates direction, see libusb_endpoint_direction.
    /// </summary>
    public libusb_endpoint_direction Direction { get; }
    public byte Raw { get; }

    [JsonConstructor]
    public UsbEndpointAddress(byte raw)
    {
        Raw = raw;
        Direction =
            (raw & 0x80) != 0
                ? libusb_endpoint_direction.LIBUSB_ENDPOINT_IN
                : libusb_endpoint_direction.LIBUSB_ENDPOINT_OUT;
        Number = (libusb_endpoint_number)(raw & 0x0F);
    }

    public override string ToString() => $"{Direction} {Number} (0x{Raw:X2})";
}
