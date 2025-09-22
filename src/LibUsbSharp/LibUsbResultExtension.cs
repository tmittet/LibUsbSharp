using LibUsbNative;

namespace LibUsbSharp;

public static class LibUsbResultExtension
{
    public static string GetMessage(this LibUsbResult result)
    {
        return LibUsbErrorMessages.Get((LibUsbError)result);
    }
}
