namespace LibUsbNative.Descriptors;

public interface IUsbInterface
{
    IReadOnlyList<IUsbInterfaceDescriptor> AlternateSettings { get; }
}
