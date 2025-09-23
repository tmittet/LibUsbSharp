namespace LibUsbNative.Enums;

/// <summary>Available option values for libusb_set_option() and libusb_init_context().</summary>
public enum libusb_option
{
    /// <summary>Set the log message verbosity.</summary>
    LIBUSB_OPTION_LOG_LEVEL = 0,

    /// <summary>Use the UsbDk backend for a specific context, if available.</summary>
    LIBUSB_OPTION_USE_USBDK = 1,

    /// <summary>Do not scan for devices.</summary>
    LIBUSB_OPTION_NO_DEVICE_DISCOVERY = 2,

    /// <summary>Set the context log callback function.</summary>
    LIBUSB_OPTION_LOG_CB = 3,
}
