using UsbDotNet.LibUsbNative.Enums;

namespace UsbDotNet.LibUsbNative.Extensions;

public static class libusb_error_Extension
{
    internal const string UnknownLibUsbErrorMessagePrefix = "Unknown libusb error";

    /// <summary>
    /// Managed libusb error -> message mapping (avoids native libusb_error call).
    /// The messages always start with a capital letter and end without any dot.
    /// </summary>
    public static string GetString(this libusb_error error) =>
        error switch
        {
            libusb_error.LIBUSB_SUCCESS => "Success",
            libusb_error.LIBUSB_ERROR_IO => "Input/Output Error",
            libusb_error.LIBUSB_ERROR_INVALID_PARAM => "Invalid parameter",
            libusb_error.LIBUSB_ERROR_ACCESS => "Access denied (insufficient permissions)",
            libusb_error.LIBUSB_ERROR_NO_DEVICE => "No such device (it may have been disconnected)",
            libusb_error.LIBUSB_ERROR_NOT_FOUND => "Entity not found",
            libusb_error.LIBUSB_ERROR_BUSY => "Resource busy",
            libusb_error.LIBUSB_ERROR_TIMEOUT => "Operation timed out",
            libusb_error.LIBUSB_ERROR_OVERFLOW => "Overflow",
            libusb_error.LIBUSB_ERROR_PIPE => "Pipe error",
            libusb_error.LIBUSB_ERROR_INTERRUPTED =>
                "System call interrupted (perhaps due to signal)",
            libusb_error.LIBUSB_ERROR_NO_MEM => "Insufficient memory",
            libusb_error.LIBUSB_ERROR_NOT_SUPPORTED =>
                "Operation not supported or unimplemented on this platform",
            libusb_error.LIBUSB_ERROR_OTHER => "Other error",
            _ => $"{UnknownLibUsbErrorMessagePrefix} {error}",
        };
}
