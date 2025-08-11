namespace LibUsbSharp.Descriptor;

/// <inheritdoc/>
public record struct UsbConfigDescriptor(
    byte ConfigId,
    byte StringDescriptionIndex,
    UsbConfigAttributes Attributes,
    byte MaxPowerRawValue,
    byte[] ExtraBytes,
    List<IUsbInterfaceDescriptor> Interfaces
) : IUsbConfigDescriptor
{
    public readonly int MaxPower => MaxPowerRawValue * 2;
}
