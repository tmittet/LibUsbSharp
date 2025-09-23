using System.Text.Json;
using LibUsbNative.Descriptors;

namespace LibUsbNative.Extensions;

public static class DescriptorToJsonExtension
{
    public static string ToJson(this UsbDeviceDescriptor deviceDescriptor) =>
        JsonSerializer.Serialize(deviceDescriptor, SerializationContext.Default.UsbDeviceDescriptor);

    public static string ToJson(this UsbConfigDescriptor configDescriptor) =>
        JsonSerializer.Serialize(configDescriptor, SerializationContext.Default.UsbConfigDescriptor);

    public static string ToJson(this UsbInterface usbInterface) =>
        JsonSerializer.Serialize(usbInterface, SerializationContext.Default.UsbInterface);

    public static string ToJson(this UsbInterfaceDescriptor interfaceDescriptor) =>
        JsonSerializer.Serialize(interfaceDescriptor, SerializationContext.Default.UsbInterfaceDescriptor);

    public static string ToJson(this UsbEndpointDescriptor endpointDescriptor) =>
        JsonSerializer.Serialize(endpointDescriptor, SerializationContext.Default.UsbEndpointDescriptor);
}
