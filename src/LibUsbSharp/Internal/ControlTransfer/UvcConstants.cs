using System;

namespace LibUsbSharp.Internal.ControlTransfer;

/// <summary>
/// bmRequestType is an 8-bit field, split as follows:
/// Bit 7 (Direction) | 0 = Host → Device (OUT) 1 = Device → Host (IN)
/// Bits 6..5 (Type)  | 00 = Standard 01 = Class 10 = Vendor 11 = Reserved Bits
/// 4..0 (Recipient)  | 00000 = Device 00001 = Interface 00010 = Endpoint 00011 = Other
/// </summary>
public enum BmRequestType : byte
{
    // Device -> Host | Class | Interface
    GetInterface = 0b10100001,

    // Host -> Device | Class | Interface
    SetInterface = 0b00100001,
}

public enum BRequest : byte
{
    SetCurrent = 0x01,
    GetCurrent = 0x81,
    GetMin = 0x82,
    GetMax = 0x83,
    GetResolution = 0x84,
    GetLength = 0x85,
    GetInfo = 0x86,
    GetDef = 0x87,
}

public enum Selector : byte
{
    Brightness = 0x02,
    Contrast = 0x03,
}

public static class VideoSubclass
{
    public const byte VideoControl = 0x01;
}

public static class UvcConst
{
    public const byte USB_CLASS_VIDEO = 0x0E;
    public const byte CS_INTERFACE = 0x24;
    public const byte VC_INPUT_TERMINAL = 0x02;
    public const byte VC_PROCESSING_UNIT = 0x05;
    public const byte VC_EXTENSION_UNIT = 0x06;
    public const ushort ITT_CAMERA = 0x0201;
}

public static class PU_CONTROLS
{
    public const byte BRIGHTNESS = 0x02;
    public const byte CONTRAST = 0x03;
}
