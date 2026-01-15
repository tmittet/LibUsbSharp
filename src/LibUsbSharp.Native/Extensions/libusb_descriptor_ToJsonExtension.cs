using System.Text.Json;
using LibUsbSharp.Native.Structs;

namespace LibUsbSharp.Native.Extensions;

public static class libusb_descriptor_ToJsonExtension
{
    public static string ToJson(this libusb_device_descriptor deviceDescriptor) =>
        JsonSerializer.Serialize(
            deviceDescriptor,
            LibUsbSharpNativeSerializationContext.Default.libusb_device_descriptor
        );

    public static string ToJson(this libusb_config_descriptor configDescriptor) =>
        JsonSerializer.Serialize(
            configDescriptor,
            LibUsbSharpNativeSerializationContext.Default.libusb_config_descriptor
        );

    public static string ToJson(this libusb_interface usbInterface) =>
        JsonSerializer.Serialize(usbInterface, LibUsbSharpNativeSerializationContext.Default.libusb_interface);

    public static string ToJson(this libusb_interface_descriptor interfaceDescriptor) =>
        JsonSerializer.Serialize(
            interfaceDescriptor,
            LibUsbSharpNativeSerializationContext.Default.libusb_interface_descriptor
        );

    public static string ToJson(this libusb_endpoint_descriptor endpointDescriptor) =>
        JsonSerializer.Serialize(
            endpointDescriptor,
            LibUsbSharpNativeSerializationContext.Default.libusb_endpoint_descriptor
        );
}
