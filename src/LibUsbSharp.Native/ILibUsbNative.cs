using LibUsbSharp.Native.Enums;
using LibUsbSharp.Native.SafeHandles;
using LibUsbSharp.Native.Structs;

namespace LibUsbSharp.Native;

public interface ILibUsbNative
{
    ISafeContext CreateContext();

    libusb_version GetVersion();

    bool HasCapability(libusb_capability capability);

    string StrError(libusb_error usbError);
}
