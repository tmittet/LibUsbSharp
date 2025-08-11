namespace LibUsbSharp.Internal.Transfer;

internal enum LibUsbTransferStatus : int
{
    /// <summary>
    /// Transfer completed without error (or not started).
    /// This does not indicate that the entire amount of requested data was transferred.
    /// </summary>
    Completed = 0,
    Error,
    TimedOut,
    Cancelled,

    /// <summary>
    /// For bulk/interrupt endpoints: halt condition detected (endpoint stalled).
    /// For control endpoints: control request not supported.
    /// </summary>
    Stall,

    /// <summary>
    /// Device was disconnected.
    /// </summary>
    NoDevice,

    /// <summary>
    /// Device sent more data than requested.
    /// </summary>
    Overflow,
}
