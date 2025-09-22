using System.Runtime.InteropServices;

namespace LibUsbNative.Descriptors;

#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable IDE1006 // Naming Styles

[StructLayout(LayoutKind.Sequential)]
internal struct native_libusb_interface
{
    public IntPtr altsetting;
    public int num_altsetting;
}

#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore IDE1006 // Naming Styles
