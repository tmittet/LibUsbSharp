namespace LibUsbNative.Enums;

public enum UsbDescriptorType : byte
{
    Device = 0x01,
    Configuration = 0x02,
    Interface = 0x04,
#pragma warning disable CA1720 // Identifier contains type name
    String = 0x03,
#pragma warning restore CA1720 // Identifier contains type name
    Endpoint = 0x05,
    DeviceQualifier = 0x06,
    OtherSpeedConfiguration = 0x07,
    InterfacePower = 0x08,
    BOS = 0x0F,
    DeviceCapability = 0x10,
    SuperspeedUsbEndpointCompanion = 0x30,
    SuperspeedPlusIsochronousEndpointCompanion = 0x31,
}
