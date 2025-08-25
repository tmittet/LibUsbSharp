namespace LibUsbSharp;

/// <summary>
/// This enum represents the control request direction. The enum value should be bitwise combined
/// with ControlRequestType and ControlRequestRecipient, to form the full ControlRequest type.
/// </summary>
internal enum ControlRequestDirection : byte
{
    Out = 0b00000000,
    In = 0b10000000, // 0x80
}

public enum ControlRequestType : byte
{
    /// <summary>
    /// Request value per the standard control requests defined in the USB spec.
    /// </summary>
    Standard = 0b00000000,

    /// <summary>
    /// Request values defined in the individual USB class spec.
    /// </summary>
    Class = 0b00100000, // 0x20

    /// <summary>
    /// Request values defined by device vendor.
    /// </summary>
    Vendor = 0b01000000, // 0x40
}

public enum ControlRequestRecipient : byte
{
    /// <summary>
    /// The request affects the whole device.
    /// </summary>
    Device = 0b00000000,

    /// <summary>
    /// The request targets a specific interface.
    /// </summary>
    Interface = 0b00000001, // 0x01

    /// <summary>
    /// The request targets a specific endpoint.
    /// </summary>
    Endpoint = 0b00000010, // 0x02

    /// <summary>
    /// The request targets "other" elements defined by a class spec (not an interface or endpoint).
    /// </summary>
    Other = 0b00000011, // 0x03
}
