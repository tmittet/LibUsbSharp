using System.Runtime.InteropServices;

namespace LibUsbNative.Descriptors;

[StructLayout(LayoutKind.Sequential)]
public struct TimeVal
{
    public nint tv_sec;
    public nint tv_usec;
}
