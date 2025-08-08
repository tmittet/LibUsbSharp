using System.Runtime.InteropServices;

namespace LibUsbSharp.Internal;

[StructLayout(LayoutKind.Sequential)]
internal struct LibUsbVersion
{
    public ushort Major;
    public ushort Minor;
    public ushort Micro;
    public ushort Nano;

    [MarshalAs(UnmanagedType.LPStr)]
    public string DllVersion;

    [MarshalAs(UnmanagedType.LPStr)]
    public string Rc;

    public override readonly string ToString()
    {
        return $"{Major}.{Minor}.{Micro}.{Nano}";
    }
}
