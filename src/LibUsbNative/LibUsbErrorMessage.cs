using LibUsbNative.Enums;

namespace LibUsbNative;

/// <summary>Managed libusb error -> message mapping (avoids native strerror call).</summary>
public static class LibUsbErrorMessage
{
    private static readonly Dictionary<LibUsbError, string> Map = new()
    {
        { LibUsbError.Success, "Success." },
        { LibUsbError.Io, "Input/output error." },
        { LibUsbError.InvalidParam, "Invalid parameter." },
        { LibUsbError.Access, "Access denied (insufficient permissions)." },
        { LibUsbError.NoDevice, "No such device (it may have been disconnected)." },
        { LibUsbError.NotFound, "Entity not found." },
        { LibUsbError.Busy, "Resource busy." },
        { LibUsbError.Timeout, "Operation timed out." },
        { LibUsbError.Overflow, "Overflow (device sent more data than requested)." },
        { LibUsbError.Pipe, "Pipe error (stall)." },
        { LibUsbError.Interrupted, "System call interrupted (retry may succeed)." },
        { LibUsbError.NoMem, "Insufficient memory." },
        { LibUsbError.NotSupported, "Operation not supported or unimplemented on this platform." },
        { LibUsbError.Other, "Other / unspecified libusb error." },
    };

    public static string Get(LibUsbError error) =>
        Map.TryGetValue(error, out var msg) ? msg : $"Unknown libusb error ({(int)error}).";
}
