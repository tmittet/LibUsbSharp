using System.Text.Json;
using System.Text.Json.Serialization;
using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

/// <summary>
/// Strongly typed view of endpoint bmAttributes.
/// </summary>
[JsonConverter(typeof(UsbEndpointAttributesFlexibleJsonConverter))]
public readonly struct UsbEndpointAttributes
{
    public UsbEndpointTransferType TransferType { get; }
    public UsbIsochronousSyncType SyncType { get; }
    public UsbIsochronousUsageType UsageType { get; }
    public byte Raw { get; }

    internal UsbEndpointAttributes(byte raw)
    {
        Raw = raw;
        TransferType = (UsbEndpointTransferType)(raw & 0x03);
        SyncType = (UsbIsochronousSyncType)((raw >> 2) & 0x03);
        UsageType = (UsbIsochronousUsageType)((raw >> 4) & 0x03);
    }

    public override string ToString() => $"Transfer={TransferType}, Sync={SyncType}, Usage={UsageType}, Raw=0x{Raw:X2}";
}

// -------------------------------------------------
// Flexible JSON converters (for default JsonSerializer usage)
// These ensure deserialization works even without custom options.
// -------------------------------------------------
//
// TODO: Remove this; looks legacy
internal sealed class UsbEndpointAttributesFlexibleJsonConverter : JsonConverter<UsbEndpointAttributes>
{
    public override UsbEndpointAttributes Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Number)
            return new UsbEndpointAttributes(reader.GetByte());

        if (reader.TokenType == JsonTokenType.String)
        {
            if (TryParseByte(reader.GetString()!, out var b))
                return new UsbEndpointAttributes(b);
            return default;
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            byte? raw = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;
                var name = reader.GetString();
                reader.Read();
                if (name == "raw")
                {
                    if (reader.TokenType == JsonTokenType.Number)
                        raw = reader.GetByte();
                    else if (reader.TokenType == JsonTokenType.String && TryParseByte(reader.GetString()!, out var rb))
                        raw = rb;
                }
            }
            return new UsbEndpointAttributes(raw ?? 0);
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, UsbEndpointAttributes value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Raw);

    private static bool TryParseByte(string s, out byte b)
    {
        s = s.Trim();
        int idx = s.IndexOf('(');
        if (idx >= 0)
            s = s[(idx + 1)..].TrimEnd(')').Trim(); // support "(0xNN)"
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return byte.TryParse(s[2..], System.Globalization.NumberStyles.HexNumber, null, out b);
        return byte.TryParse(s, out b);
    }
}
