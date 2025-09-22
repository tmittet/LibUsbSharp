using System.Runtime.InteropServices;

namespace LibUsbNative.Descriptors;

#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable IDE1006 // Naming Styles

[StructLayout(LayoutKind.Sequential)]
internal struct native_libusb_endpoint_descriptor
{
    public byte bLength;
    public byte bDescriptorType;
    public byte bEndpointAddress;
    public byte bmAttributes;
    public ushort wMaxPacketSize;
    public byte bInterval;
    public byte bRefresh;
    public byte bSynchAddress;
    public IntPtr extra;
    public int extra_length;
}

#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore IDE1006 // Naming Styles
