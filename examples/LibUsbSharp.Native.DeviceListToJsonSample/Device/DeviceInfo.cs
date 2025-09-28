using LibUsbSharp.Native.Structs;

namespace LibUsbSharp.Native.DeviceListToJsonSample.Device;

internal sealed record DeviceInfo(
    libusb_device_descriptor Descriptor,
    byte BusNumber,
    byte DeviceAddress,
    byte PortNumber,
    DeviceStringDescriptor[] StringDescriptors,
    libusb_config_descriptor[] ConfigDescriptors
);
