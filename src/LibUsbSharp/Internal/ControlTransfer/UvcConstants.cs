using System;

namespace LibUsbSharp.Internal.ControlTransfer;

/// <summary>
/// bmRequestType is an 8-bit field, split as follows:
/// Bit 7 (Direction) | 0 = Host → Device (OUT) 1 = Device → Host (IN)
/// Bits 6..5 (Type)  | 00 = Standard 01 = Class 10 = Vendor 11 = Reserved Bits
/// 4..0 (Recipient)  | 00000 = Device 00001 = Interface 00010 = Endpoint 00011 = Other
/// </summary>
internal static class BmRequestType
{
    // Device -> Host | Class | Interface
    public const byte GetInterface = 0b10100001;

    // Host -> Device | Class | Interface
    public const byte SetInterface = 0b00100001;
}

internal static class BRequest
{
    public const byte SetCurrent = 0x01;
    public const byte GetCurrent = 0x81;
    public const byte GetMin = 0x82;
    public const byte GetMax = 0x83;
    public const byte GetResolution = 0x84;
    public const byte GetLength = 0x85;
    public const byte GetInfo = 0x86;
    public const byte GetDef = 0x87;
}

internal static class ProcessingUnitSelectors
{
    public const byte Brightness = 0x02;
    public const byte Contrast = 0x03;
}

public static class UvcConst
{
    // Class-specific VC descriptor tags
    public const byte USB_CLASS_VIDEO = 0x0E;
    public const byte SC_VIDEOCONTROL = 0x01;
    public const byte CS_INTERFACE = 0x24;
    public const byte VC_INPUT_TERMINAL = 0x02;
    public const byte VC_PROCESSING_UNIT = 0x05;
    public const byte VC_EXTENSION_UNIT = 0x06;

    public const ushort ITT_CAMERA = 0x0201;

    public static class PU
    {
        public const byte BRIGHTNESS = 0x02;
        public const byte CONTRAST = 0x03;
    }
}
