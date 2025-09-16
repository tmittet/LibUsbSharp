using System.Runtime.InteropServices;
using LibUsbNative;
using LibUsbNative.SafeHandles;

namespace LibUsbNative;

public interface ILibUsbNative
{
    ISafeContext CreateContext();
    LibUsbVersion GetVersion();
    bool HasCapability(uint capability);
    string StrError(LibUsbError error);

    static ILibUsbNative Init(ILibUsbApi? api = null)
    {
        var libusb = new LibUsbNative(api ?? new PInvokeLibUsbApi());
        return libusb;
    }
}
