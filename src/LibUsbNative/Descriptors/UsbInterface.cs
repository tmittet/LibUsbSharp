namespace LibUsbNative.Descriptors;

// TODO: Fix this
#pragma warning disable SYSLIB1037

public record UsbInterface(UsbInterfaceDescriptor[] AlternateSettings) : IUsbInterface
{
    IReadOnlyList<IUsbInterfaceDescriptor> IUsbInterface.AlternateSettings => Array.AsReadOnly(AlternateSettings);
}

#pragma warning restore SYSLIB1037
