using System.Runtime.InteropServices;

namespace LibUsbNative.Descriptors;

[StructLayout(LayoutKind.Sequential)]
public struct libusb_timeval
{
    // TODO: Should these be int64? Do they depend on the OS?
    public nint tv_sec;
    public nint tv_usec;

    public readonly TimeSpan TimeSpan => TimeSpan.FromSeconds(tv_sec) + TimeSpan.FromTicks(tv_usec * 10);
}
