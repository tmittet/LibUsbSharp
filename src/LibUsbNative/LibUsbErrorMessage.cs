using LibUsbNative.Enums;

namespace LibUsbNative;

/// <summary>Managed libusb error -> message mapping (avoids native strerror call).</summary>
public static class LibUsbErrorMessage
{
    private static readonly Dictionary<libusb_error, string> Map = new()
    {
        { libusb_error.LIBUSB_SUCCESS, "Success." },
        { libusb_error.LIBUSB_ERROR_IO, "Input/output error." },
        { libusb_error.LIBUSB_ERROR_INVALID_PARAM, "Invalid parameter." },
        { libusb_error.LIBUSB_ERROR_ACCESS, "Access denied (insufficient permissions)." },
        { libusb_error.LIBUSB_ERROR_NO_DEVICE, "No such device (it may have been disconnected)." },
        { libusb_error.LIBUSB_ERROR_NOT_FOUND, "Entity not found." },
        { libusb_error.LIBUSB_ERROR_BUSY, "Resource busy." },
        { libusb_error.LIBUSB_ERROR_TIMEOUT, "Operation timed out." },
        { libusb_error.LIBUSB_ERROR_OVERFLOW, "Overflow (device sent more data than requested)." },
        { libusb_error.LIBUSB_ERROR_PIPE, "Pipe error (stall)." },
        { libusb_error.LIBUSB_ERROR_INTERRUPTED, "System call interrupted (retry may succeed)." },
        { libusb_error.LIBUSB_ERROR_NO_MEM, "Insufficient memory." },
        { libusb_error.LIBUSB_ERROR_NOT_SUPPORTED, "Operation not supported or unimplemented on this platform." },
        { libusb_error.LIBUSB_ERROR_OTHER, "Other / unspecified libusb error." },
    };

    public static string Get(libusb_error error) =>
        Map.TryGetValue(error, out var msg) ? msg : $"Unknown libusb error ({(int)error}).";
}
