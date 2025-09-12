namespace LibUsbNative;

/// <summary>
/// libusb return codes. Most libusb functions return 0 on success or a negative error code.
/// </summary>
public enum LibUsbError : int
{
    Success = 0,

    /// <summary>Input/output error.</summary>
    Io = -1,

    /// <summary>Invalid parameter.</summary>
    InvalidParam = -2,

    /// <summary>Access denied (insufficient permissions).</summary>
    Access = -3,

    /// <summary>No such device (it may have been disconnected).</summary>
    NoDevice = -4,

    /// <summary>Entity not found.</summary>
    NotFound = -5,

    /// <summary>Resource busy.</summary>
    Busy = -6,

    /// <summary>Operation timed out.</summary>
    Timeout = -7,

    /// <summary>Overflow (device sent more data than requested).</summary>
    Overflow = -8,

    /// <summary>Pipe error (stall).</summary>
    Pipe = -9,

    /// <summary>System call was interrupted (retry might succeed).</summary>
    Interrupted = -10,

    /// <summary>Insufficient memory.</summary>
    NoMem = -11,

    /// <summary>Operation not supported or unimplemented on this platform.</summary>
    NotSupported = -12,

    // -99 is reserved as a catch-all
    Other = -99,
}
