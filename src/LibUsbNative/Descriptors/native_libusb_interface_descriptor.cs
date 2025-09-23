using System.Runtime.InteropServices;

namespace LibUsbNative.Descriptors;

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
