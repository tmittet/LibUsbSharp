namespace LibUsbSharp.Internal.Hotplug;

internal enum LibUsbHotplugFlag
{
    None = 0x00,

    /// <summary>
    /// Arm the callback and fire it for all matching currently attached devices.
    /// </summary>
    Enumerate = 0x01,
}
