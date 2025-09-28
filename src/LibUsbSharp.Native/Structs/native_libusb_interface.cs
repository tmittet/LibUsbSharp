using System.Runtime.InteropServices;

namespace LibUsbSharp.Native.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct native_libusb_interface
{
    public IntPtr altsetting;
    public int num_altsetting;
}
