namespace LibUsbSharp.Internal;

internal enum LibUsbOption
{
    /// <summary>
    /// Set LibUsb log message verbosity. LibUsb recommends LibUsbLogLevel.Warning.
    ///
    /// If the LIBUSB_DEBUG environment variable was set when libusb was initialized,
    /// this option does nothing: the message verbosity is fixed to the value in the
    /// environment variable.
    /// </summary>
    LogLevel = 0,

    /// <summary>
    /// Use the UsbDk backend for a specific context, if available.
    ///
    /// Only valid on Windows. Ignored on all other platforms.
    /// </summary>
    UseUsbdk = 1,

    /// <summary>
    /// Do not scan for devices during intit.Hotplug functionality will also be deactivated.
    ///
    /// Only valid on Linux. Ignored on all other platforms.
    /// </summary>
    NoDeviceDiscovery = 2,

    /// <summary>
    /// Set the context log callback function.
    /// </summary>
    LogCallback = 3,
}
