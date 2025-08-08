namespace LibUsbSharp;

public enum LibUsbResult : int
{
    Success = 0,

    //
    // Errors that originate from LibUsb
    //

    /// <summary>
    /// Input/output error
    /// </summary>
    IoError = -1,

    /// <summary>
    /// Invalid parameter
    /// </summary>
    InvalidParameter = -2,

    /// <summary>
    /// Access denied (insufficient permissions)
    /// </summary>
    AccessDenied = -3,

    /// <summary>
    /// No such device (it may have been disconnected)
    /// </summary>
    NoDevice = -4,

    /// <summary>
    /// Entity not found
    /// </summary>
    NotFound = -5,

    /// <summary>
    /// Resource busy
    /// </summary>
    ResourceBusy = -6,

    /// <summary>
    /// Operation timed out
    /// </summary>
    Timeout = -7,

    /// <summary>
    /// Overflow
    /// </summary>
    Overflow = -8,

    /// <summary>
    /// Pipe error
    /// </summary>
    PipeError = -9,

    /// <summary>
    /// System call interrupted (perhaps due to signal)
    /// </summary>
    Interrupted = -10,

    /// <summary>
    /// Insufficient memory
    /// </summary>
    InsufficientMemory = -11,

    /// <summary>
    /// Operation not supported or unimplemented on this platform
    /// </summary>
    NotSupported = -12,

    /// <summary>
    /// Other error
    /// </summary>
    OtherError = -99,

    //
    // Errors that originate from managed code
    //

    /// <summary>
    /// Generic error in the native USB library managed code wrapper
    /// </summary>
    ManagedError = OtherError << 16,
}
