using System.Text.Json.Serialization;
using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

/// <summary>
/// Combined representation of bEndpointAddress.
/// </summary>
public readonly record struct libusb_endpoint_address
{
    /// <summary>
    /// Bits 0:3 of the rawValue are the endpoint number.
    /// </summary>
    public libusb_endpoint_number Number { get; }

    /// <summary>
    /// Bit 7 of the rawValue indicates direction, see libusb_endpoint_direction.
    /// </summary>
    public libusb_endpoint_direction Direction { get; }

    /// <summary>
    /// Raw value of bEndpointAddress.
    /// </summary>
    public byte rawValue { get; }

    [JsonConstructor]
    public libusb_endpoint_address(byte rawValue)
    {
        this.rawValue = rawValue;
        Direction =
            (rawValue & 0x80) != 0
                ? libusb_endpoint_direction.LIBUSB_ENDPOINT_IN
                : libusb_endpoint_direction.LIBUSB_ENDPOINT_OUT;
        Number = (libusb_endpoint_number)(rawValue & 0x0F);
    }

    public override string ToString() => $"{Direction} {Number} (0x{rawValue:X2})";
}
