namespace LibUsbSharp.Descriptor;

public interface IUsbConfigDescriptor
{
    /// <summary>
    /// Identifier value for this configuration.
    /// </summary>
    byte ConfigId { get; }

    /// <summary>
    /// Index of string descriptor describing this configuration.
    /// </summary>
    byte StringDescriptionIndex { get; }

    /// <summary>
    /// Configuration characteristics.
    /// </summary>
    UsbConfigAttributes Attributes { get; }

    /// <summary>
    /// Maximum milliampere power consumption of the USB device from the
    /// bus in this configuration, when the device is fully operation.
    /// </summary>
    int MaxPower { get; }

    /// <summary>
    /// Extra configuration bytes.
    /// </summary>
    byte[] ExtraBytes { get; }

    /// <summary>
    /// A list of interfaces (libusb_interface) supported by this configuration.
    /// </summary>
    List<IUsbInterfaceDescriptor> Interfaces { get; }
}
