using System.Runtime.InteropServices;
using LibUsbNative;

namespace LibUsbSharp;

public static class LibUsbResultExtension
{
    public static string GetMessage(this LibUsbResult result)
    {
        return LibUsbNative.LibUsbNative.StrError((LibUsbError)result);
    }
}
