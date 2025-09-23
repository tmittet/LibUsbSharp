using System.Text.Json;
using System.Text.Json.Serialization;
using LibUsbNative.Enums;

namespace LibUsbNative.Descriptors;

/// <summary>
/// Combined representation of bEndpointAddress.
/// </summary>
[JsonConverter(typeof(UsbEndpointAddressFlexibleJsonConverter))]
public readonly record struct UsbEndpointAddress
{
    public UsbEndpointNumber Number { get; }
    public UsbEndpointDirection Direction { get; }
    public byte Raw { get; }

    [JsonConstructor]
    public UsbEndpointAddress(byte raw)
    {
        Raw = raw;
        Direction = (raw & 0x80) != 0 ? UsbEndpointDirection.In : UsbEndpointDirection.Out;
        Number = (UsbEndpointNumber)(raw & 0x0F);
    }

    public override string ToString() => $"{Direction} {Number} (0x{Raw:X2})";
}

// -------------------------------------------------
// Flexible JSON converters (for default JsonSerializer usage)
// These ensure deserialization works even without custom options.
// -------------------------------------------------
//
// TODO: Remove this; looks legacy

internal sealed class UsbEndpointAddressFlexibleJsonConverter : JsonConverter<UsbEndpointAddress>
{
    public override UsbEndpointAddress Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Accept number
        if (reader.TokenType == JsonTokenType.Number)
            return new UsbEndpointAddress(reader.GetByte());

        // Accept string (decimal or hex, optionally with decorations)
        if (reader.TokenType == JsonTokenType.String)
        {
            if (TryParseByte(reader.GetString()!, out var b))
                return new UsbEndpointAddress(b);
            return default;
        }

        // Accept legacy object shapes { "raw": n } or { "direction":"In ...", "number":"Ep1 ..." }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            byte? raw = null;
            UsbEndpointDirection? dir = null;
            byte? num = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;
                var name = reader.GetString();
                reader.Read();
                switch (name)
                {
                    case "raw":
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            raw = reader.GetByte();
                        }
                        else if (
                            reader.TokenType == JsonTokenType.String
                            && TryParseByte(reader.GetString()!, out var rb)
                        )
                        {
                            raw = rb;
                        }

                        break;
                    case "direction":
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            var s = StripDecor(reader.GetString()!);
                            if (Enum.TryParse<UsbEndpointDirection>(s, true, out var edir))
                                dir = edir;
                        }
                        break;
                    case "number":
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            num = reader.GetByte();
                        }
                        else if (
                            reader.TokenType == JsonTokenType.String
                            && TryParseByte(reader.GetString()!, out var nb)
                        )
                        {
                            num = nb;
                        }

                        break;
                }
            }

            if (raw is { } r)
                return new UsbEndpointAddress(r);

            if (num is { } n && dir is { } d)
            {
                byte v = (byte)(n & 0x0F);
                if (d == UsbEndpointDirection.In)
                    v |= 0x80;
                return new UsbEndpointAddress(v);
            }
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, UsbEndpointAddress value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Raw);

    private static bool TryParseByte(string s, out byte b)
    {
        s = StripDecor(s);
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return byte.TryParse(s[2..], System.Globalization.NumberStyles.HexNumber, null, out b);
        return byte.TryParse(s, out b);
    }

    private static string StripDecor(string s)
    {
        s = s.Trim();
        int idx = s.IndexOf('(');
        if (idx >= 0)
            s = s[..idx].Trim();
        return s;
    }
}
