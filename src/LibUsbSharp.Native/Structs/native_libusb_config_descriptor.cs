using System.Runtime.InteropServices;

namespace LibUsbSharp.Native.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct native_libusb_config_descriptor
{
    public byte bLength;
    public byte bDescriptorType;
    public ushort wTotalLength;
    public byte bNumInterfaces;
    public byte bConfigurationValue;
    public byte iConfiguration;
    public byte bmAttributes;
    public byte MaxPower;
    public IntPtr interfacePtr;
    public IntPtr extra;
    public int extra_length;
}
