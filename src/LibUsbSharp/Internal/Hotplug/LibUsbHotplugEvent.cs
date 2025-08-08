namespace LibUsbSharp.Internal.Hotplug;

internal enum LibUsbHotplugEvent : int
{
    DeviceArrived = 0x01,
    DeviceLeft = 0x02,
}
