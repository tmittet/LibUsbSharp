using System.Runtime.InteropServices;

namespace LibUsbNative.Descriptors;

#pragma warning disable CA1707 // Identifiers should not contain underscores

[StructLayout(LayoutKind.Sequential)]
public struct TimeVal
{
    public nint tv_sec;
    public nint tv_usec;
}

#pragma warning restore CA1707 // Identifiers should not contain underscores
