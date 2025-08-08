using System.Runtime.InteropServices;

namespace LibUsbSharp.Internal.Descriptor;

[StructLayout(LayoutKind.Sequential)]
internal struct LibUsbInterface
{
    /// <summary>
    /// Pointer to array of interface descriptors.
    /// </summary>
    public nint AltSetting;

    /// <summary>
    /// The number of alternate settings that belong to this interface.
    /// </summary>
    public int NumAltSetting;

    public readonly List<LibUsbInterfaceDescriptor> GetAltInterfaceList()
    {
        var altInterfaceList = new List<LibUsbInterfaceDescriptor>();
        var interfaceByteSize = Marshal.SizeOf<LibUsbInterfaceDescriptor>();
        for (var i = 0; i < NumAltSetting; i++)
        {
            var interfaceHandle = new IntPtr(AltSetting + (i * interfaceByteSize));
            altInterfaceList.Add(
                Marshal.PtrToStructure<LibUsbInterfaceDescriptor>(interfaceHandle)
            );
        }
        return altInterfaceList;
    }
}
