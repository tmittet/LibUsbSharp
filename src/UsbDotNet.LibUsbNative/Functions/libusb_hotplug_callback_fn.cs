using System.Runtime.InteropServices;
using LibUsbSharp.Native.Enums;

namespace LibUsbSharp.Native.Functions;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate libusb_hotplug_return libusb_hotplug_callback_fn(
    nint ctx,
    nint device,
    libusb_hotplug_event event_type,
    nint user_data
);
