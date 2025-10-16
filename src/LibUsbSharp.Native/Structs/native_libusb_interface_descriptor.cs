using System.Runtime.InteropServices;

namespace LibUsbSharp.Native.Structs;

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
    public nint endpoint;
    public nint extra;
    public int extra_length;

    public readonly IEnumerable<native_libusb_endpoint_descriptor> ReadEndpoints()
    {
        var interfaceByteSize = Marshal.SizeOf<native_libusb_endpoint_descriptor>();
        for (var i = 0; i < bNumEndpoints; i++)
        {
            var interfaceHandle = IntPtr.Add(endpoint, i * interfaceByteSize);
            yield return Marshal.PtrToStructure<native_libusb_endpoint_descriptor>(interfaceHandle);
        }
    }
}
