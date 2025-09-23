using System.Text.Json.Serialization;

namespace LibUsbNative.Descriptors;

public readonly record struct UsbInterface
{
    public IReadOnlyList<UsbInterfaceDescriptor> AlternateSettings { get; }

    [JsonConstructor]
    public UsbInterface(IReadOnlyList<UsbInterfaceDescriptor> alternateSettings)
    {
        AlternateSettings = alternateSettings;
    }
}
