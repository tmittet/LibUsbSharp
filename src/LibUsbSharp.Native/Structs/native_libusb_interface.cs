using System.Runtime.InteropServices;

namespace LibUsbSharp.Native.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct native_libusb_interface
{
    public nint altsetting;
    public int num_altsetting;

    public readonly IEnumerable<native_libusb_interface_descriptor> ReadAltSettings()
    {
        var interfaceByteSize = Marshal.SizeOf<native_libusb_interface_descriptor>();
        for (var i = 0; i < num_altsetting; i++)
        {
            var interfaceHandle = IntPtr.Add(altsetting, i * interfaceByteSize);
            yield return Marshal.PtrToStructure<native_libusb_interface_descriptor>(interfaceHandle);
        }
    }
}
