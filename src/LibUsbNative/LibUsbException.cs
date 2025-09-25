using LibUsbNative.Enums;

namespace LibUsbNative;

public sealed class LibUsbException : Exception
{
    public libusb_error Error { get; }

    // Build the message from the optional user message + mapped error text
    public override string Message
    {
        get
        {
            var mapped = LibUsbErrorMessage.Get(Error);
            return string.IsNullOrWhiteSpace(base.Message) ? mapped : $"{base.Message} {mapped}".Trim();
        }
    }

    public LibUsbException(libusb_error error, string? message)
        : base(message) => Error = error;

    public static void ThrowIfError(libusb_error rc, string? msg = null)
    {
        if (rc >= 0)
            return;
        throw new LibUsbException(rc, msg);
    }
}
