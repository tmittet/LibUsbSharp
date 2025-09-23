using System.Runtime.InteropServices;

namespace LibUsbNative.Descriptors;

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
