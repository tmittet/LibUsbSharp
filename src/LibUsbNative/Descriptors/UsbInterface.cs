using System.Text.Json.Serialization;

namespace LibUsbNative.Descriptors;

public readonly record struct UsbInterface
{
    public IReadOnlyList<libusb_interface_descriptor> AlternateSettings { get; }

    [JsonConstructor]
    public UsbInterface(IReadOnlyList<libusb_interface_descriptor> alternateSettings)
    {
        AlternateSettings = alternateSettings;
    }
}
