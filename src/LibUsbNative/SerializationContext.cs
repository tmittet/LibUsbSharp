using System.Text.Json.Serialization;
using LibUsbNative.Descriptors;
using LibUsbNative.Enums;

namespace LibUsbNative;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UsbDeviceDescriptor))]
[JsonSerializable(typeof(UsbConfigDescriptor))]
[JsonSerializable(typeof(UsbInterface))]
[JsonSerializable(typeof(UsbInterfaceDescriptor))]
[JsonSerializable(typeof(UsbEndpointDescriptor))]
[JsonSerializable(typeof(UsbEndpointAddress))]
[JsonSerializable(typeof(UsbEndpointAttributes))]
[JsonSerializable(typeof(UsbConfigAttributes))] // Added
[JsonSerializable(typeof(UsbConfigDescriptor[]))]
[JsonSerializable(typeof(UsbInterface[]))]
[JsonSerializable(typeof(UsbInterfaceDescriptor[]))]
[JsonSerializable(typeof(UsbEndpointDescriptor[]))]
[JsonSerializable(typeof(List<UsbConfigDescriptor>))]
[JsonSerializable(typeof(List<UsbInterfaceDescriptor>))]
[JsonSerializable(typeof(List<UsbEndpointDescriptor>))]
[JsonSerializable(typeof(IReadOnlyList<UsbConfigDescriptor>))]
[JsonSerializable(typeof(IReadOnlyList<UsbInterfaceDescriptor>))]
[JsonSerializable(typeof(IReadOnlyList<UsbEndpointDescriptor>))]
[JsonSerializable(typeof(IEnumerable<UsbConfigDescriptor>))]
[JsonSerializable(typeof(IEnumerable<UsbInterfaceDescriptor>))]
[JsonSerializable(typeof(IEnumerable<UsbEndpointDescriptor>))]
internal partial class UsbJsonContextIndented : JsonSerializerContext { }

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(UsbDeviceDescriptor))]
[JsonSerializable(typeof(UsbConfigDescriptor))]
[JsonSerializable(typeof(UsbInterface))]
[JsonSerializable(typeof(UsbInterfaceDescriptor))]
[JsonSerializable(typeof(UsbEndpointDescriptor))]
[JsonSerializable(typeof(UsbEndpointAddress))]
[JsonSerializable(typeof(UsbEndpointAttributes))]
[JsonSerializable(typeof(UsbConfigDescriptor[]))]
[JsonSerializable(typeof(UsbInterface[]))]
[JsonSerializable(typeof(UsbInterfaceDescriptor[]))]
[JsonSerializable(typeof(UsbEndpointDescriptor[]))]
[JsonSerializable(typeof(List<UsbConfigDescriptor>))]
[JsonSerializable(typeof(List<UsbInterfaceDescriptor>))]
[JsonSerializable(typeof(List<UsbEndpointDescriptor>))]
[JsonSerializable(typeof(IReadOnlyList<UsbConfigDescriptor>))]
[JsonSerializable(typeof(IReadOnlyList<UsbInterfaceDescriptor>))]
[JsonSerializable(typeof(IReadOnlyList<UsbEndpointDescriptor>))]
[JsonSerializable(typeof(IEnumerable<UsbConfigDescriptor>))]
[JsonSerializable(typeof(IEnumerable<UsbInterfaceDescriptor>))]
[JsonSerializable(typeof(IEnumerable<UsbEndpointDescriptor>))]
internal partial class UsbJsonContextCompact : JsonSerializerContext { }
