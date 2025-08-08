namespace LibUsbSharp.Internal.Descriptor;

internal enum LibUsbDescriptorType : byte
{
    Device = 0x01,
    Config = 0x02,
    String = 0x03,
    Interface = 0x04,
    Endpoint = 0x05,
    DeviceQualifier = 0x6,
    OtherSpeedConfiguration = 0x7,
    InterfacePower = 0x8,
    OTG = 0x9,
    Debug = 0xA,
    InterfaceAssociation = 0xB,
    BOS = 0x0F,
    Capability = 0x10,
    Hid = 0x21,
    HidReport = 0x22,
    Physical = 0x23,
    Hub = 0x29,
    SuperspeedHub = 0x2A,
    SuperspeedEndpointCompanion = 0x30,
}
