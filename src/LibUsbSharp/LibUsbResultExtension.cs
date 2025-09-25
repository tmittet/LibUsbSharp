using LibUsbNative;
using LibUsbNative.Enums;

namespace LibUsbSharp;

public static class LibUsbResultExtension
{
    public static string GetMessage(this LibUsbResult result)
    {
        return LibUsbErrorMessage.Get((libusb_error)result);
    }
}
