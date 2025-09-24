using LibUsbNative.Enums;
using LibUsbNative.SafeHandles;

namespace LibUsbNative.Extensions;

public static class SafeContextExtension
{
    public static void SetOption(this ISafeContext safeContext, libusb_log_level value) =>
        safeContext.SetOption(libusb_option.LIBUSB_OPTION_LOG_LEVEL, (int)value);
}
