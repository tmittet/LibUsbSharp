using LibUsbNative.Enums;
using LibUsbNative.SafeHandles;
using LibUsbNative.Structs;

namespace LibUsbNative;

public interface ILibUsbNative
{
    ISafeContext CreateContext();

    libusb_version GetVersion();

    bool HasCapability(libusb_capability capability);

    string StrError(libusb_error usbError);
}
