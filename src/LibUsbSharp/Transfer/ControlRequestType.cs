namespace LibUsbSharp.Transfer;

/// <summary>
/// This enum represents the control request direction. The enum value should be bitwise combined
/// with ControlRequestType and ControlRequestRecipient, to form the full ControlRequest type.
/// </summary>
internal enum ControlRequestDirection : byte
{
    Out = 0b0,
    In = 0b1,
}

public enum ControlRequestType : byte
{
    /// <summary>
    /// Request value per the standard control requests defined in the USB spec.
    /// </summary>
    Standard = 0b00,

    /// <summary>
    /// Request values defined in the individual USB class spec.
    /// </summary>
    Class = 0b01,

    /// <summary>
    /// Request values defined by device vendor.
    /// </summary>
    Vendor = 0b10,
}

public enum ControlRequestRecipient : byte
{
    /// <summary>
    /// The request affects the whole device.
    /// </summary>
    Device = 0b00000,

    /// <summary>
    /// The request targets a specific interface.
    /// </summary>
    Interface = 0b00001, // 0x01

    /// <summary>
    /// The request targets a specific endpoint.
    /// </summary>
    Endpoint = 0b00010, // 0x02

    /// <summary>
    /// The request targets "other" elements defined by a class spec (not an interface or endpoint).
    /// </summary>
    Other = 0b00011, // 0x03
}

public abstract record ControlRequestRequest
{
    // The raw bRequest value
    public abstract byte RawRequest { get; }
    public abstract ushort RawValue { get; }
    public abstract ushort RawIndex { get; }
    public abstract ControlRequestType RawType { get; }

    // Variants
    public sealed record Standard(ControlRequestStandard Request, ushort Value, ushort Index) : ControlRequestRequest
    {
        public override byte RawRequest => (byte)Request;
        public override ushort RawValue => Value;
        public override ushort RawIndex => Index;
        public override ControlRequestType RawType => ControlRequestType.Standard;
        public override string ToString() => $"Standard(0x{Value:X2})";
    }

    public sealed record Class(byte Request, ushort Value, ushort Index) : ControlRequestRequest
    {
        public override byte RawRequest => Request;
        public override ushort RawValue => Value;
        public override ushort RawIndex => Index;
        public override ControlRequestType RawType => ControlRequestType.Class;

        public override string ToString() => $"Class(0x{Request:X2})";
    }

    public sealed record Vendor(byte Request, ushort Value, ushort Index) : ControlRequestRequest
    {
        public override byte RawRequest => Request;
        public override ushort RawValue => Value;
        public override ushort RawIndex => Index;
        public override ControlRequestType RawType => ControlRequestType.Vendor;

        public override string ToString() => $"Vendor(0x{Value:X2})";
    }
    /*
        public sealed record Reserved(byte Request, ushort Value, ushort Index) : ControlRequestRequest
        {
            //public override byte Code => Value;
            public override string ToString() => $"Reserved(0x{Value:X2})";
        }
    */
}
