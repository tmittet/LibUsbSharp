using System.Text.Json;
using LibUsbSharp.Native.Structs;

namespace LibUsbSharp.Native.Extensions;

public static class DescriptorToJsonExtension
{
    public static string ToJson(this libusb_device_descriptor deviceDescriptor) =>
        JsonSerializer.Serialize(deviceDescriptor, SerializationContext.Default.libusb_device_descriptor);

    public static string ToJson(this libusb_config_descriptor configDescriptor) =>
        JsonSerializer.Serialize(configDescriptor, SerializationContext.Default.libusb_config_descriptor);

    public static string ToJson(this libusb_interface usbInterface) =>
        JsonSerializer.Serialize(usbInterface, SerializationContext.Default.libusb_interface);

    public static string ToJson(this libusb_interface_descriptor interfaceDescriptor) =>
        JsonSerializer.Serialize(interfaceDescriptor, SerializationContext.Default.libusb_interface_descriptor);

    public static string ToJson(this libusb_endpoint_descriptor endpointDescriptor) =>
        JsonSerializer.Serialize(endpointDescriptor, SerializationContext.Default.libusb_endpoint_descriptor);
}
