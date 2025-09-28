namespace LibUsbSharp.Native.Enums;

/// <summary>
/// Bitwise or of hotplug events that will trigger the callback.
/// </summary>
[Flags]
public enum libusb_hotplug_event : int
{
    NONE = 0,
    LIBUSB_HOTPLUG_EVENT_DEVICE_ARRIVED = 0x01,
    LIBUSB_HOTPLUG_EVENT_DEVICE_LEFT = 0x02,
}
