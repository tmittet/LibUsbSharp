using LibUsbNative.Enums;
using LibUsbNative.SafeHandles;

namespace LibUsbNative;

public interface ILibUsbNative
{
    ISafeContext CreateContext();

    LibUsbVersion GetVersion();

    bool HasCapability(uint capability);

    string StrError(libusb_error usbError);
}
