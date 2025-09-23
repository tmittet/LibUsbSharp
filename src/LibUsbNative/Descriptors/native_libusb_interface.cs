using System.Runtime.InteropServices;

namespace LibUsbNative.Descriptors;

[StructLayout(LayoutKind.Sequential)]
internal struct native_libusb_interface
{
    public IntPtr altsetting;
    public int num_altsetting;
}
