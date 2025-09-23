using System.Text.Json.Serialization;
using LibUsbNative.Descriptors;

namespace LibUsbNative;

[JsonSourceGenerationOptions(
    // TODO: Converters not available in .NET6
    //Converters = new[]
    //{
    //    typeof(JsonStringEnumConverter<LibUsbError>),
    //},
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    // TODO: IndentSize not available in .NET6
    //IndentSize = 2,
    WriteIndented = true
)]
[JsonSerializable(typeof(libusb_config_descriptor))]
[JsonSerializable(typeof(libusb_config_descriptor[]))]
[JsonSerializable(typeof(IReadOnlyList<libusb_config_descriptor>))]
[JsonSerializable(typeof(libusb_device_descriptor))]
[JsonSerializable(typeof(libusb_endpoint_address))]
[JsonSerializable(typeof(libusb_endpoint_attributes))]
[JsonSerializable(typeof(UsbEndpointDescriptor))]
[JsonSerializable(typeof(UsbEndpointDescriptor[]))]
[JsonSerializable(typeof(IReadOnlyList<UsbEndpointDescriptor>))]
[JsonSerializable(typeof(UsbInterface))]
[JsonSerializable(typeof(UsbInterface[]))]
[JsonSerializable(typeof(IReadOnlyList<UsbInterface>))]
[JsonSerializable(typeof(libusb_interface_descriptor))]
[JsonSerializable(typeof(libusb_interface_descriptor[]))]
[JsonSerializable(typeof(IReadOnlyList<libusb_interface_descriptor>))]
internal partial class SerializationContext : JsonSerializerContext { }
