namespace LibUsbNative.Enums;

public enum UsbEndpointTransferType : byte
{
    Control = 0,
    Isochronous = 1,
    Bulk = 2,
    Interrupt = 3,
}
