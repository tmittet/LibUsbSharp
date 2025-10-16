using System.Runtime.InteropServices;

namespace LibUsbSharp.Native.Structs;

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
    public nint extra;
    public int extra_length;
}
