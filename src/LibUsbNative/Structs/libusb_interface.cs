using System.Text.Json.Serialization;

namespace LibUsbNative.Structs;

/// <summary>
/// A collection of alternate settings for a particular USB interface.
/// </summary>
public readonly record struct libusb_interface
{
    /// <summary>
    /// Array of interface descriptors.
    /// </summary>
    public IReadOnlyList<libusb_interface_descriptor> altsetting { get; }

    [JsonConstructor]
    public libusb_interface(IReadOnlyList<libusb_interface_descriptor> altsetting)
    {
        this.altsetting = altsetting;
    }
}
