using System;
using System.Runtime.CompilerServices;

namespace LibUsbNative;

/// <summary>libusb return codes. Most functions return 0 (Success) or a negative error.</summary>
/*
public enum LibUsbError
{
    Success = 0,
    Io = -1,
    InvalidParam = -2,
    Access = -3,
    NoDevice = -4,
    NotFound = -5,
    Busy = -6,
    Timeout = -7,
    Overflow = -8,
    Pipe = -9,
    Interrupted = -10,
    NoMem = -11,
    NotSupported = -12,
    Other = -99,
}
*/

public sealed class LibUsbException : Exception
{
    public LibUsbError Error { get; }
    public override string Message => $"{base.Message} {LibUsb.StrError(Error)}".Trim();

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
