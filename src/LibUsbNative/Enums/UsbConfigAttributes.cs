namespace LibUsbNative.Enums;

[Flags]
public enum UsbConfigAttributes : byte
{
    None = 0,
    RemoteWakeup = 0x20,
    SelfPowered = 0x40,
    MustBeSet = 0x80,
}
