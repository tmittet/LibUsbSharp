using System.Runtime.InteropServices;

namespace LibUsbSharp.Internal.Descriptor;

[StructLayout(LayoutKind.Sequential)]
internal struct LibUsbInterfaceDescriptor
{
    /// <summary>
    /// Size of this descriptor (in bytes).
    /// </summary>
    public byte Length;

    /// <summary>
    /// Descriptor type.
    /// </summary>
    public LibUsbDescriptorType DescriptorType;

    /// <summary>
    /// Number of this interface.
    /// </summary>
    public byte InterfaceNumber;

    /// <summary>
    /// Value used to select this alternate setting for this interface.
    /// </summary>
    public byte AlternateSetting;

    /// <summary>
    /// Number of endpoints used by this interface (excluding the control endpoint).
    /// </summary>
    public byte NumEndpoints;

    /// <summary>
    /// USB-IF class code for this interface.
    /// </summary>
    public UsbClass InterfaceClass;

    /// <summary>
    /// USB-IF subclass code for this interface, qualified by the bInterfaceClass value.
    /// </summary>
    public byte InterfaceSubClass;

    /// <summary>
    /// USB-IF protocol code for this interface, qualified by the bInterfaceClass and bInterfaceSubClass values
    /// </summary>
    public byte InterfaceProtocol;

    /// <summary>
    /// Index of string descriptor describing this interface.
    /// </summary>
    public byte Interface;

    /// <summary>
    /// A pointer to an array of endpoint descriptors.(libusb_endpoint_descriptor).
    /// </summary>
    public nint Endpoint;

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
    /// A list of endpoint descriptors.
    /// </summary>
    public readonly List<LibUsbEndpointDescriptor> GetEndpointList()
    {
        var endpointList = new List<LibUsbEndpointDescriptor>();
        var endpointByteSize = Marshal.SizeOf<LibUsbEndpointDescriptor>();
        for (var i = 0; i < NumEndpoints; i++)
        {
            var endpointHandle = new IntPtr(Endpoint + (i * endpointByteSize));
            endpointList.Add(Marshal.PtrToStructure<LibUsbEndpointDescriptor>(endpointHandle));
        }
        return endpointList;
    }
}
