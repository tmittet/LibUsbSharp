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
[JsonSerializable(typeof(UsbConfigDescriptor))]
[JsonSerializable(typeof(UsbConfigDescriptor[]))]
[JsonSerializable(typeof(IReadOnlyList<UsbConfigDescriptor>))]
[JsonSerializable(typeof(UsbDeviceDescriptor))]
[JsonSerializable(typeof(UsbEndpointAddress))]
[JsonSerializable(typeof(UsbEndpointAttributes))]
[JsonSerializable(typeof(UsbEndpointDescriptor))]
[JsonSerializable(typeof(UsbEndpointDescriptor[]))]
[JsonSerializable(typeof(IReadOnlyList<UsbEndpointDescriptor>))]
[JsonSerializable(typeof(UsbInterface))]
[JsonSerializable(typeof(UsbInterface[]))]
[JsonSerializable(typeof(IReadOnlyList<UsbInterface>))]
[JsonSerializable(typeof(UsbInterfaceDescriptor))]
[JsonSerializable(typeof(UsbInterfaceDescriptor[]))]
[JsonSerializable(typeof(IReadOnlyList<UsbInterfaceDescriptor>))]
internal partial class SerializationContext : JsonSerializerContext { }
