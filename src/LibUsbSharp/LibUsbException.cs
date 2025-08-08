namespace LibUsbSharp;

public sealed class LibUsbException : Exception
{
    public LibUsbResult ResultCode { get; }

    public LibUsbException(string message, LibUsbResult result = LibUsbResult.OtherError)
        : base(message)
    {
        ResultCode = result;
    }

    internal static LibUsbException FromResult(LibUsbResult result, string? message = null) =>
        new($"{message} {result.GetMessage()}".Trim(), result);

    internal static LibUsbException FromError(int result, string? message = null) =>
        FromResult((LibUsbResult)result, message);
}
