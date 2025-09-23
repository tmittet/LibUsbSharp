using System.Text.Json.Serialization;
using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

/// <summary>
/// Strongly typed view of endpoint bmAttributes.
/// </summary>
public readonly record struct UsbEndpointAttributes
{
    public libusb_endpoint_transfer_type TransferType { get; }

    /// <summary>
    /// Synchronization type for isochronous endpoints. Bits 2:3 of the raw value.
    /// </summary>
    public libusb_iso_sync_type SyncType { get; }

    /// <summary>
    /// Usage type for isochronous endpoints. Bits 4:5 of the raw value.
    /// </summary>
    public libusb_iso_usage_type UsageType { get; }
    public byte Raw { get; }

    [JsonConstructor]
    public UsbEndpointAttributes(byte raw)
    {
        Raw = raw;
        TransferType = (libusb_endpoint_transfer_type)(raw & 0x03);
        SyncType = (libusb_iso_sync_type)((raw >> 2) & 0x03);
        UsageType = (libusb_iso_usage_type)((raw >> 4) & 0x03);
    }

    public override string ToString() => $"Transfer={TransferType}, Sync={SyncType}, Usage={UsageType}, Raw=0x{Raw:X2}";
}
