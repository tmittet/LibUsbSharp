using System.Runtime.InteropServices;

namespace LibUsbSharp.Internal.Descriptor;

[StructLayout(LayoutKind.Sequential)]
internal struct LibUsbDeviceDescriptor
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
    /// USB specification release number in binary-coded decimal.
    /// </summary>
    public ushort BcdUsb;

    /// <summary>
    /// USB-IF class code for the device.
    /// </summary>
    public UsbClass DeviceClass;

    /// <summary>
    /// USB-IF subclass code for the device, qualified by the bDeviceClass value.
    /// </summary>
    public byte DeviceSubClass;

    /// <summary>
    /// USB-IF protocol code for the device, qualified by the bDeviceClass and bDeviceSubClass values.
    /// </summary>
    public byte DeviceProtocol;

    /// <summary>
    /// Maximum packet size for endpoint 0.
    /// </summary>
    public byte MaxPacketSize0;

    /// <summary>
    /// USB-IF vendor ID.
    /// </summary>
    public ushort VendorId;

    /// <summary>
    /// USB-IF product ID.
    /// </summary>
    public ushort ProductId;

    /// <summary>
    /// Device release number in binary-coded decimal.
    /// </summary>
    public ushort BcdDevice;

    /// <summary>
    /// Index of string descriptor describing manufacturer.
    /// </summary>
    public byte Manufacturer;

    /// <summary>
    /// Index of string descriptor describing product.
    /// </summary>
    public byte Product;

    /// <summary>
    /// Index of string descriptor containing device serial number.
    /// </summary>
    public byte SerialNumber;

    /// <summary>
    /// Number of possible configurations.
    /// </summary>
    public byte NumConfigurations;
}
