using System.Runtime.CompilerServices;

namespace LibUsbNative;

/// <summary>Managed libusb error -> message mapping (avoids native strerror call).</summary>
public static class LibUsbErrorMessages
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

public sealed class LibUsbException : Exception
{
    public LibUsbError Error { get; }

    // Build the message from the optional user message + mapped error text
    public override string Message
    {
        get
        {
            var mapped = LibUsbErrorMessages.Get(Error);
            if (string.IsNullOrWhiteSpace(base.Message))
                return mapped;
            return $"{base.Message} {mapped}".Trim();
        }
    }

    public LibUsbException(LibUsbError error, string? message)
        : base(message) => Error = error;

    public static void ThrowIfError(LibUsbError rc, string? msg = null)
    {
        if (rc >= 0)
            return;
        throw new LibUsbException(rc, msg);
    }
}

public static class LibUsbErrorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSuccess(this int rc) => rc == 0;

    public static bool IsTransient(this LibUsbError e) =>
        e is LibUsbError.Timeout or LibUsbError.Interrupted or LibUsbError.Busy or LibUsbError.Io;
}
