namespace UsbDotNet.Transfer;

public enum ControlRequestType : byte
{
    /// <summary>
    /// Request value per the standard control requests defined in the USB spec.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Request values defined in the individual USB class spec.
    /// </summary>
    Class = 1,

    /// <summary>
    /// Request values defined by device vendor.
    /// </summary>
    Vendor = 2,
}
