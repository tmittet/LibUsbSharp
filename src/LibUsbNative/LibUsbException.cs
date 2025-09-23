using LibUsbNative.Enums;

namespace LibUsbNative;

public sealed class LibUsbException : Exception
{
    public LibUsbError Error { get; }

    // Build the message from the optional user message + mapped error text
    public override string Message
    {
        get
        {
            var mapped = LibUsbErrorMessage.Get(Error);
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
