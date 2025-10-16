using LibUsbSharp.Descriptor;
using LibUsbSharp.Native.Structs;

namespace LibUsbSharp.Internal;

internal static class LibUsbDescriptorExtension
{
    internal static IUsbConfigDescriptor ToUsbInterfaceDescriptor(this libusb_config_descriptor descriptor) =>
        new UsbConfigDescriptor(
            ConfigId: descriptor.bConfigurationValue,
            StringDescriptionIndex: descriptor.iConfiguration,
            Attributes: (UsbConfigAttributes)descriptor.bmAttributes,
            MaxPowerRawValue: descriptor.bMaxPower,
            ExtraBytes: descriptor.extra,
            // TODO: Interfaces should be Dictionary<int, List<IUsbInterfaceDescriptor>>, something like
            // descriptor.interfaces
            //   .Select((i, index) => (index, value: i.altsetting.Select(a => a.ToUsbInterfaceDescriptor()).ToList()))
            //   .ToDictionary(t => t.index, t => t.value)
            Interfaces: descriptor
                .interfaces.SelectMany(i => i.altsetting)
                .Select(a => a.ToUsbInterfaceDescriptor())
                .ToList()
        );

    internal static IUsbInterfaceDescriptor ToUsbInterfaceDescriptor(this libusb_interface_descriptor descriptor) =>
        new UsbInterfaceDescriptor(
            InterfaceNumber: descriptor.bInterfaceNumber,
            AlternateSetting: descriptor.bAlternateSetting,
            InterfaceClass: (UsbClass)descriptor.bInterfaceClass,
            InterfaceSubClass: descriptor.bInterfaceSubClass,
            InterfaceProtocol: descriptor.bInterfaceProtocol,
            StringDescriptionIndex: descriptor.iInterface,
            ExtraBytes: descriptor.extra,
            Endpoints: descriptor.endpoints.Select(e => e.ToUsbEndpointDescriptor()).ToList()
        );

    internal static IUsbEndpointDescriptor ToUsbEndpointDescriptor(this libusb_endpoint_descriptor descriptor) =>
        new UsbEndpointDescriptor(
            EndpointAddress: new UsbEndpointAddress(descriptor.bEndpointAddress.RawValue),
            Attributes: new UsbEndpointAttributes(descriptor.bmAttributes.RawValue),
            MaxPacketSize: descriptor.wMaxPacketSize,
            Interval: descriptor.bInterval,
            Refresh: descriptor.bRefresh,
            SynchAddress: descriptor.bSynchAddress,
            ExtraBytes: descriptor.extra
        );
}
