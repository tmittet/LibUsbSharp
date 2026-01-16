using System.Runtime.InteropServices;
using LibUsbSharp.Native.Enums;

namespace LibUsbSharp.Native.Functions;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void libusb_log_cb(nint ctx, libusb_log_level level, string str);
