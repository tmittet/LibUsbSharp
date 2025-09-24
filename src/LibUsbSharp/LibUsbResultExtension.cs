using LibUsbNative.Enums;
using LibUsbNative.Extensions;

namespace LibUsbSharp;

public static class LibUsbResultExtension
{
    public static string GetMessage(this LibUsbResult result) => ((libusb_error)result).GetString() + '.';
}
