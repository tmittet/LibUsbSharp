namespace LibUsbSharp.Internal.Transfer;

internal enum LibUsbTransferType : byte
{
    /// <summary>
    /// For sending control commands to device, e.g., setting configuration.
    /// </summary>
    Control = 0,

    /// <summary>
    /// For real-time streaming of data, e.g., audio or video.
    /// </summary>
    Isochronous = 1,

    /// <summary>
    /// For large, non-time-critical data transfers, e.g., file storage.
    /// </summary>
    Bulk = 2,

    /// <summary>
    /// For small amounts of data that must be transferred quickly, e.g., HID data.
    /// </summary>
    Interrupt = 3,
}
