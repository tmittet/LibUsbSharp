namespace LibUsbSharp.Extensions.Uvc;

public enum ControlRequestUvc : byte
{
    SetCurrentSetting = 0x01,
    GetCurrentSetting = 0x81,
    GetMinimumValue = 0x82,
    GetMaximumValue = 0x83,
    GetResolution = 0x84,
    GetLength = 0x85,
    GetInfo = 0x86,
    GetDefault = 0x87,
}
