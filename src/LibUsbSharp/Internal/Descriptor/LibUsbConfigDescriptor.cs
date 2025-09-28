using System.Runtime.InteropServices;
using LibUsbSharp.Native.Enums;

namespace LibUsbSharp.Internal.Descriptor;

[StructLayout(LayoutKind.Sequential)]
internal struct LibUsbConfigDescriptor
{
    /// <summary>
    /// Size of this descriptor (in bytes).
    /// </summary>
    public byte Length;

    /// <summary>
    /// Descriptor type.
    /// </summary>
    public libusb_descriptor_type DescriptorType;

    /// <summary>
    /// Total length of data returned for this configuration.
    /// </summary>
    public ushort TotalLength;

    /// <summary>
    /// Number of interfaces supported by this configuration.
    /// </summary>
    public byte NumInterfaces;

    /// <summary>
    /// Identifier value for this configuration.
    /// </summary>
    public byte ConfigurationValue;

    /// <summary>
    /// Index of string descriptor describing this configuration.
    /// </summary>
    public byte Configuration;

    /// <summary>
    /// Configuration characteristics.
    /// </summary>
    public byte Attributes;

    /// <summary>
    /// Maximum power consumption of the USB device from this bus
    /// in this configuration when the device is fully operation.
    /// </summary>
    public byte MaxPower;

    /// <summary>
    /// A pointer to an array of interfaces (libusb_interface) supported by this configuration.
    /// </summary>
    public nint Interface;

    /// <summary>
    /// Extra descriptors.
    /// </summary>
    public nint Extra;

    /// <summary>
    /// Length of the extra descriptors, in bytes.
    /// </summary>
    public int ExtraLength;

    /// <summary>
    /// Extra descriptor bytes.
    /// </summary>
    public readonly byte[] GetExtraBytes()
    {
        if (Extra == IntPtr.Zero)
        {
            return Array.Empty<byte>();
        }
        var bytes = new byte[ExtraLength];
        Marshal.Copy(Extra, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// A list of interfaces (libusb_interface) supported by this configuration.
    /// </summary>
    public readonly List<LibUsbInterface> GetInterfaceList()
    {
        var interfaceList = new List<LibUsbInterface>();
        var interfaceByteSize = Marshal.SizeOf<LibUsbInterface>();
        for (var i = 0; i < NumInterfaces; i++)
        {
            var interfaceHandle = new IntPtr(Interface + (i * interfaceByteSize));
            interfaceList.Add(Marshal.PtrToStructure<LibUsbInterface>(interfaceHandle));
        }
        return interfaceList;
    }
}
