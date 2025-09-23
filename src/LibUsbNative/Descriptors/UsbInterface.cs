namespace LibUsbNative.Descriptors;

public record class UsbInterface() : IUsbInterface
{
    public IReadOnlyList<IUsbInterfaceDescriptor> AlternateSettings { get; set; } =
        Array.Empty<UsbInterfaceDescriptor>();
}
