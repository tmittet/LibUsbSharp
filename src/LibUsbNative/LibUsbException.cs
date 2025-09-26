using System.Diagnostics;
using LibUsbNative.Enums;
using LibUsbNative.Extensions;

namespace LibUsbNative;

public sealed class LibUsbException : Exception
{
    public libusb_error Error { get; }

    // Build the message from the optional user message + mapped error text
    public override string Message
    {
        get
        {
            var message = $"{Error}: {Error.GetString()}.";
            return string.IsNullOrWhiteSpace(base.Message) ? message : $"{base.Message} {message}";
        }
    }

    public LibUsbException(libusb_error error, string? message)
        : base(message)
    {
        Error = error;
    }

    public static LibUsbException FromError(libusb_error result, string? message = null) =>
        throw new LibUsbException(result, message);

    public static LibUsbException FromApiError(libusb_error result, string methodName, string? message = null) =>
        new(result, $"LibUsbApi '{methodName}' failed. {message}".TrimEnd());

    [StackTraceHidden]
    internal static void ThrowIfApiError(libusb_error result, string methodName, string? message = null)
    {
        if (result >= 0)
        {
            return;
        }
        throw FromApiError(result, methodName, message);
    }
}
