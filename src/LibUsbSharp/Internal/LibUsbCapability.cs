namespace LibUsbSharp.Internal;

internal enum LibUsbCapability : uint
{
    /// <summary>
    /// Hotplug support is available on this platform.
    /// </summary>
    HasHotplug = 0x0001,
    /// <summary>
    /// The library can access HID devices without requiring user intervention.
    /// </summary>
    HasHidAccess = 0x0100,
    /// <summary>
    /// The library supports detaching of the default USB driver,
    /// using libusb_detach_kernel_driver(), if one is set by the OS kernel.
    /// </summary>
    SupportsDetachKernelDriver = 0x0101,
}
