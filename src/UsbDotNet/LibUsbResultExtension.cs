using UsbDotNet.LibUsbNative.Enums;
using UsbDotNet.LibUsbNative.Extensions;

namespace UsbDotNet;

public static class LibUsbResultExtension
{
    public static string GetMessage(this LibUsbResult result) => ((libusb_error)result).GetString() + '.';
}
