using LibUsbSharp.Descriptor;

namespace LibUsbSharp.Internal.Descriptor;

internal static class LibUsbDescriptorExtension
{
    public static IUsbConfigDescriptor ToUsbInterfaceDescriptor(this LibUsbConfigDescriptor descriptor) =>
        new UsbConfigDescriptor(
            ConfigId: descriptor.ConfigurationValue,
            StringDescriptionIndex: descriptor.Configuration,
            Attributes: (UsbConfigAttributes)descriptor.Attributes,
            MaxPowerRawValue: descriptor.MaxPower,
            ExtraBytes: descriptor.GetExtraBytes(),
            Interfaces: descriptor
                .GetInterfaceList()
                .SelectMany(i => i.GetAltInterfaceList())
                .Select(a => a.ToUsbInterfaceDescriptor())
                .ToList()
        );

    public static IUsbInterfaceDescriptor ToUsbInterfaceDescriptor(this LibUsbInterfaceDescriptor descriptor) =>
        new UsbInterfaceDescriptor(
            InterfaceNumber: descriptor.InterfaceNumber,
            AlternateSetting: descriptor.AlternateSetting,
            InterfaceClass: descriptor.InterfaceClass,
            InterfaceSubClass: descriptor.InterfaceSubClass,
            InterfaceProtocol: descriptor.InterfaceProtocol,
            StringDescriptionIndex: descriptor.Interface,
            ExtraBytes: descriptor.GetExtraBytes(),
            Endpoints: descriptor.GetEndpointList().Select(e => e.ToUsbEndpointDescriptor()).ToList()
        );

    public static IUsbEndpointDescriptor ToUsbEndpointDescriptor(this LibUsbEndpointDescriptor descriptor) =>
        new UsbEndpointDescriptor(
            EndpointAddress: descriptor.EndpointAddress,
            Attributes: descriptor.Attributes,
            MaxPacketSize: descriptor.MaxPacketSize,
            Interval: descriptor.Interval,
            Refresh: descriptor.Refresh,
            SynchAddress: descriptor.SynchAddress,
            ExtraBytes: descriptor.GetExtraBytes()
        );
}
