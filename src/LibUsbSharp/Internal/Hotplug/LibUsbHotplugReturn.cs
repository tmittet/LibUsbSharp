﻿namespace LibUsbSharp.Internal.Hotplug;

/// <summary>
/// A callback function must return an int (0 or 1) indicating whether the callback is expecting
/// additional events. See: https://libusb.sourceforge.io/api-1.0/libusb_hotplug.html
/// </summary>
internal enum LibUsbHotplugReturn : int
{
    /// <summary>
    /// Rearm the callback.
    /// </summary>
    Rearm = 0,

    /// <summary>
    /// Deregister the callback. NOTE: When callbacks are called from
    /// libusb_hotplug_register_callback() the callback return value is ignored.
    /// </summary>
    Deregister = 1,
}
