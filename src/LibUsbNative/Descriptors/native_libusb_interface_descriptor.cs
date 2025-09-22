using System.Runtime.InteropServices;

namespace LibUsbNative.Descriptors;

#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable IDE1006 // Naming Styles

[StructLayout(LayoutKind.Sequential)]
internal struct native_libusb_interface_descriptor
{
    public byte bLength;
    public byte bDescriptorType;
    public byte bInterfaceNumber;
    public byte bAlternateSetting;
    public byte bNumEndpoints;
    public byte bInterfaceClass;
    public byte bInterfaceSubClass;
    public byte bInterfaceProtocol;
    public byte iInterface;
    public IntPtr endpoint;
    public IntPtr extra;
    public int extra_length;
}

#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore IDE1006 // Naming Styles
