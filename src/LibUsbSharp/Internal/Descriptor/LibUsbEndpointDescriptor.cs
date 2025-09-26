using System.Runtime.InteropServices;
using LibUsbNative.Enums;
using LibUsbSharp.Descriptor;

namespace LibUsbSharp.Internal.Descriptor;

[StructLayout(LayoutKind.Sequential)]
internal struct LibUsbEndpointDescriptor
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
    /// The address of the endpoint described by this descriptor.
    /// </summary>
    public byte EndpointAddress;

    public readonly UsbEndpointDirection EndpointDirection =>
        EndpointAddress < 0x80 ? UsbEndpointDirection.Output : UsbEndpointDirection.Input;

    /// <summary>
    /// Attributes which apply to the endpoint when it is configured using the bConfigurationValue.
    /// </summary>
    public byte Attributes;

    /// <summary>
    /// Maximum packet size this endpoint is capable of sending/receiving.
    /// </summary>
    public ushort MaxPacketSize;

    /// <summary>
    /// Interval for polling endpoint for data transfers.
    /// </summary>
    public byte Interval;

    /// <summary>
    /// For audio devices only: the rate at which synchronization feedback is provided.
    /// </summary>
    public byte Refresh;

    /// <summary>
    /// For audio devices only: the address if the synch endpoint.
    /// </summary>
    public byte SynchAddress;

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
}
