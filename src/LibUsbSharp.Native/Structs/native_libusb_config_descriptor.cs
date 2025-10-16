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
    public nint interfacePtr;
    public nint extra;
    public int extra_length;

    public readonly IEnumerable<native_libusb_interface> ReadInterfaces()
    {
        var interfaceByteSize = Marshal.SizeOf<native_libusb_interface>();
        for (var i = 0; i < bNumInterfaces; i++)
        {
            var interfaceHandle = IntPtr.Add(interfacePtr, i * interfaceByteSize);
            yield return Marshal.PtrToStructure<native_libusb_interface>(interfaceHandle);
        }
    }
}
