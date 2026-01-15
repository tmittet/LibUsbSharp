namespace LibUsbSharp.Descriptor;

public enum UsbEndpointDirection
{
    // UsbEndpointAddress.RawValue 0x00-0x7F
    Output,

    // UsbEndpointAddress.RawValue 0x80-0xFF
    Input,
}
