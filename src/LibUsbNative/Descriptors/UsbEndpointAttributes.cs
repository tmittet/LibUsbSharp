using System.Text.Json.Serialization;
using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

/// <summary>
/// Strongly typed view of endpoint bmAttributes.
/// </summary>
public readonly record struct UsbEndpointAttributes
{
    public UsbEndpointTransferType TransferType { get; }
    public UsbIsochronousSyncType SyncType { get; }
    public UsbIsochronousUsageType UsageType { get; }
    public byte Raw { get; }

    [JsonConstructor]
    public UsbEndpointAttributes(byte raw)
    {
        Raw = raw;
        TransferType = (UsbEndpointTransferType)(raw & 0x03);
        SyncType = (UsbIsochronousSyncType)((raw >> 2) & 0x03);
        UsageType = (UsbIsochronousUsageType)((raw >> 4) & 0x03);
    }

    public override string ToString() => $"Transfer={TransferType}, Sync={SyncType}, Usage={UsageType}, Raw=0x{Raw:X2}";
}
