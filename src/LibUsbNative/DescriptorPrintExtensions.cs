using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibUsbNative;
using LibUsbNative.Descriptors;

namespace LibUsbNative.Descriptor;

#pragma warning disable CA1305 // Specify IFormatProvider

public static class DescriptorPrintExtensions
{
    // -----------------------
    // Tree/structured printing (unchanged – still rich / human readable)
    // -----------------------
    public static string ToTreeString(this IUsbDeviceDescriptor d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Device Descriptor:");
        sb.AppendLine($"  bLength              : {d.BLength}");
        sb.AppendLine($"  bDescriptorType      : {Fmt(d.BDescriptorType)}");
        sb.AppendLine($"  bcdUSB               : 0x{d.BcdUSB:X4}");
        sb.AppendLine($"  bDeviceClass         : {Fmt(d.BDeviceClass)}");
        sb.AppendLine($"  bDeviceSubClass      : 0x{d.BDeviceSubClass:X2}");
        sb.AppendLine($"  bDeviceProtocol      : 0x{d.BDeviceProtocol:X2}");
        sb.AppendLine($"  bMaxPacketSize0      : {d.BMaxPacketSize0}");
        sb.AppendLine($"  idVendor             : 0x{d.IdVendor:X4}");
        sb.AppendLine($"  idProduct            : 0x{d.IdProduct:X4}");
        sb.AppendLine($"  bcdDevice            : 0x{d.BcdDevice:X4}");
        sb.AppendLine($"  iManufacturer        : {d.IManufacturer}");
        sb.AppendLine($"  iProduct             : {d.IProduct}");
        sb.AppendLine($"  iSerialNumber        : {d.ISerialNumber}");
        sb.AppendLine($"  bNumConfigurations   : {d.BNumConfigurations}");
        return sb.ToString().TrimEnd();
    }

    public static string ToTreeString(this IUsbDeviceDescriptor d, IReadOnlyList<IUsbConfigDescriptor> configs)
    {
        var sb = new StringBuilder();
        sb.AppendLine(d.ToTreeString());
        for (int i = 0; i < configs.Count; i++)
        {
            sb.AppendLine();
            sb.Append(configs[i].ToTreeString().Indent(2));
        }
        return sb.ToString().TrimEnd();
    }

    public static string ToTreeString(this IUsbConfigDescriptor cfg)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Configuration Descriptor:");
        sb.AppendLine($"  bLength             : {cfg.BLength}");
        sb.AppendLine($"  bDescriptorType     : {Fmt(cfg.BDescriptorType)}");
        sb.AppendLine($"  wTotalLength        : {cfg.WTotalLength}");
        sb.AppendLine($"  bNumInterfaces      : {cfg.BNumInterfaces}");
        sb.AppendLine($"  bConfigurationValue : {cfg.BConfigurationValue}");
        sb.AppendLine($"  iConfiguration      : {cfg.IConfiguration}");
        sb.AppendLine($"  bmAttributes        : {Fmt(cfg.BmAttributes)}");
        sb.AppendLine($"  MaxPower            : {cfg.MaxPower} (units of 2mA)");
        if (cfg.Extra is { Length: > 0 })
            sb.AppendLine($"  Extra               : {cfg.Extra.Length} bytes");
        for (int i = 0; i < cfg.Interfaces.Count; i++)
        {
            var iface = cfg.Interfaces[i];
            sb.AppendLine();
            sb.AppendLine($"  Interface[{i}]:");
            for (int a = 0; a < iface.AlternateSettings.Count; a++)
            {
                var alt = iface.AlternateSettings[a];
                sb.Append(alt.ToTreeString().Indent(4));
            }
        }
        return sb.ToString().TrimEnd();
    }

    public static string ToTreeString(this IUsbInterfaceDescriptor id)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Interface Descriptor:");
        sb.AppendLine($"  bLength           : {id.BLength}");
        sb.AppendLine($"  bDescriptorType   : {Fmt(id.BDescriptorType)}");
        sb.AppendLine($"  bInterfaceNumber  : {id.BInterfaceNumber}");
        sb.AppendLine($"  bAlternateSetting : {id.BAlternateSetting}");
        sb.AppendLine($"  bNumEndpoints     : {id.BNumEndpoints}");
        sb.AppendLine($"  bInterfaceClass   : {Fmt(id.BInterfaceClass)}");
        sb.AppendLine($"  bInterfaceSubClass: 0x{id.BInterfaceSubClass:X2}");
        sb.AppendLine($"  bInterfaceProtocol: 0x{id.BInterfaceProtocol:X2}");
        sb.AppendLine($"  iInterface        : {id.IInterface}");
        if (id.Extra is { Length: > 0 })
            sb.AppendLine($"  Extra             : {id.Extra.Length} bytes");
        foreach (var ep in id.Endpoints)
        {
            sb.AppendLine();
            sb.Append(ep.ToTreeString().Indent(2));
        }
        return sb.ToString();
    }

    public static string ToTreeString(this IUsbEndpointDescriptor ep)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Endpoint Descriptor:");
        sb.AppendLine($"  bLength          : {ep.BLength}");
        sb.AppendLine($"  bDescriptorType  : {Fmt(ep.BDescriptorType)}");
        sb.AppendLine(
            $"  bEndpointAddress : 0x{ep.BEndpointAddress.Raw:X2} ({ep.BEndpointAddress.Direction}, {ep.BEndpointAddress.Number})"
        );
        sb.AppendLine(
            $"  bmAttributes     : 0x{ep.BmAttributes.Raw:X2} ({ep.BmAttributes.TransferType}/{ep.BmAttributes.SyncType}/{ep.BmAttributes.UsageType})"
        );
        sb.AppendLine($"  wMaxPacketSize   : {ep.WMaxPacketSize}");
        sb.AppendLine($"  bInterval        : {ep.BInterval}");
        sb.AppendLine($"  bRefresh         : {ep.BRefresh}");
        sb.AppendLine($"  bSynchAddress    : {ep.BSynchAddress}");
        if (ep.Extra is { Length: > 0 })
            sb.AppendLine($"  Extra            : {ep.Extra.Length} bytes");
        return sb.ToString().TrimEnd();
    }

    // -------------
    // JSON helpers
    // -------------
    // raw: as before (numeric enums). Regardless of fancy/raw, BEndpointAddress and BmAttributes now serialize as a single byte value.
    public static string ToJson(
        this IUsbDeviceDescriptor d,
        bool indented = true,
        bool hexExtras = false,
        bool raw = false
    ) => Serialize(d, indented, hexExtras, raw);

    public static string ToJson(
        this IUsbConfigDescriptor d,
        bool indented = true,
        bool hexExtras = false,
        bool raw = false
    ) => Serialize(d, indented, hexExtras, raw);

    public static string ToJson(this IUsbInterface d, bool indented = true, bool hexExtras = false, bool raw = false) =>
        Serialize(d, indented, hexExtras, raw);

    public static string ToJson(
        this IUsbInterfaceDescriptor d,
        bool indented = true,
        bool hexExtras = false,
        bool raw = false
    ) => Serialize(d, indented, hexExtras, raw);

    public static string ToJson(
        this IUsbEndpointDescriptor d,
        bool indented = true,
        bool hexExtras = false,
        bool raw = false
    ) => Serialize(d, indented, hexExtras, raw);

    public static string ToJson<T>(
        this IEnumerable<T> descriptors,
        bool indented = true,
        bool hexExtras = false,
        bool raw = false
    ) => Serialize(descriptors, indented, hexExtras, raw);

    // -----------------------------
    // Serializer option sets
    // -----------------------------
    private static readonly JsonSerializerOptions FancyIndentedOptions = CreateFancyOptions(
        UsbJsonContextIndented.Default.Options
    );
    private static readonly JsonSerializerOptions FancyCompactOptions = CreateFancyOptions(
        UsbJsonContextCompact.Default.Options
    );
    private static readonly JsonSerializerOptions FancyIndentedHexOptions = CreateHexOptions(FancyIndentedOptions);
    private static readonly JsonSerializerOptions FancyCompactHexOptions = CreateHexOptions(FancyCompactOptions);

    private static readonly JsonSerializerOptions RawIndentedOptions = CreateRawOptions(
        UsbJsonContextIndented.Default.Options
    );
    private static readonly JsonSerializerOptions RawCompactOptions = CreateRawOptions(
        UsbJsonContextCompact.Default.Options
    );
    private static readonly JsonSerializerOptions RawIndentedHexOptions = CreateHexOptions(RawIndentedOptions);
    private static readonly JsonSerializerOptions RawCompactHexOptions = CreateHexOptions(RawCompactOptions);
    private static readonly string[] separator = new[] { "\r\n", "\n" };

    private static string Serialize<T>(T value, bool indented, bool hexExtras, bool raw)
    {
        var options = raw
            ? (
                indented
                    ? (hexExtras ? RawIndentedHexOptions : RawIndentedOptions)
                    : (hexExtras ? RawCompactHexOptions : RawCompactOptions)
            )
            : (
                indented
                    ? (hexExtras ? FancyIndentedHexOptions : FancyIndentedOptions)
                    : (hexExtras ? FancyCompactHexOptions : FancyCompactOptions)
            );

        return JsonSerializer.Serialize(value, typeof(T), options);
    }

    // Fancy (human-readable) options – still stringify enums EXCEPT BEndpointAddress/BmAttributes which are now bytes.
    private static JsonSerializerOptions CreateFancyOptions(JsonSerializerOptions baseOptions)
    {
        var o = new JsonSerializerOptions(baseOptions);
        o.Converters.Add(new UsbEndpointAddressByteConverter()); // number only
        o.Converters.Add(new UsbEndpointAttributesByteConverter()); // number only
        o.Converters.Add(new EnumWithRawJsonConverter<UsbDescriptorType>());
        o.Converters.Add(new EnumWithRawJsonConverter<UsbClass>());
        o.Converters.Add(new EnumWithRawJsonConverter<UsbConfigAttributes>());
        o.Converters.Add(new EnumWithRawJsonConverter<UsbEndpointDirection>());
        o.Converters.Add(new EnumWithRawJsonConverter<UsbEndpointNumber>());
        o.Converters.Add(new EnumWithRawJsonConverter<UsbEndpointTransferType>());
        o.Converters.Add(new EnumWithRawJsonConverter<UsbIsochronousSyncType>());
        o.Converters.Add(new EnumWithRawJsonConverter<UsbIsochronousUsageType>());
        return o;
    }

    // Raw (machine-friendly) – enums numeric, wrappers also numeric.
    private static JsonSerializerOptions CreateRawOptions(JsonSerializerOptions baseOptions)
    {
        var o = new JsonSerializerOptions(baseOptions);
        o.Converters.Add(new UsbEndpointAddressByteConverter());
        o.Converters.Add(new UsbEndpointAttributesByteConverter());
        return o;
    }

    private static JsonSerializerOptions CreateHexOptions(JsonSerializerOptions baseOptions)
    {
        var o = new JsonSerializerOptions(baseOptions);
        o.Converters.Add(new HexByteArrayConverter());
        return o;
    }

    // -----------------
    // Enum formatting helper (for tree output)
    // -----------------
    private static string Fmt<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        ulong raw = Convert.ToUInt64(value);
        string rawStr =
            raw <= 0xFF ? $"0x{raw:X2}"
            : raw <= 0xFFFF ? $"0x{raw:X4}"
            : $"0x{raw:X}";
        return $"{value} ({rawStr})";
    }

    // -----------------
    // Enum fancy converter (unchanged)
    // -----------------
    private sealed class EnumWithRawJsonConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString()!.Trim();
                int paren = s.IndexOf('(');
                if (paren >= 0)
                {
                    var inside = s[(paren + 1)..].TrimEnd(')').Trim();
                    if (TryParseNumeric(inside, out ulong val))
                        return (TEnum)Enum.ToObject(typeof(TEnum), val);
                    s = s[..paren].Trim();
                }
                if (Enum.TryParse<TEnum>(s, true, out var named))
                    return named;
                if (TryParseNumeric(s, out var val2))
                    return (TEnum)Enum.ToObject(typeof(TEnum), val2);
                return default;
            }
            if (reader.TokenType == JsonTokenType.Number)
            {
                ulong v = reader.TryGetInt64(out long l) ? unchecked((ulong)l) : (ulong)reader.GetDouble();
                return (TEnum)Enum.ToObject(typeof(TEnum), v);
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            ulong raw = Convert.ToUInt64(value);
            string rawStr =
                raw <= 0xFF ? $"0x{raw:X2}"
                : raw <= 0xFFFF ? $"0x{raw:X4}"
                : $"0x{raw:X}";
            writer.WriteStringValue($"{value} ({rawStr})");
        }

        private static bool TryParseNumeric(string s, out ulong value)
        {
            s = s.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return ulong.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
            return ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }

    // -----------------
    // Wrapper converters: numeric only (previous object shapes still accepted)
    // -----------------
    private sealed class UsbEndpointAddressByteConverter : JsonConverter<UsbEndpointAddress>
    {
        public override UsbEndpointAddress Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType == JsonTokenType.Number)
                return new UsbEndpointAddress(reader.GetByte());
            if (reader.TokenType == JsonTokenType.String && TryParseByteFlexible(reader.GetString()!, out var b))
                return new UsbEndpointAddress(b);
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
                        else if (
                            reader.TokenType == JsonTokenType.String
                            && TryParseByteFlexible(reader.GetString()!, out var rb)
                        )
                            raw = rb;
                    }
                }
                return new UsbEndpointAddress(raw ?? 0);
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, UsbEndpointAddress value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.Raw);

        private static bool TryParseByteFlexible(string s, out byte b)
        {
            s = s.Trim();
            int paren = s.IndexOf('(');
            if (paren >= 0)
                s = s[..paren].Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return byte.TryParse(s[2..], NumberStyles.HexNumber, null, out b);
            return byte.TryParse(s, out b);
        }
    }

    private sealed class UsbEndpointAttributesByteConverter : JsonConverter<UsbEndpointAttributes>
    {
        public override UsbEndpointAttributes Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType == JsonTokenType.Number)
                return new UsbEndpointAttributes(reader.GetByte());
            if (reader.TokenType == JsonTokenType.String && TryParseRaw(reader.GetString()!, out var v))
                return new UsbEndpointAttributes(v);
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
                        else if (
                            reader.TokenType == JsonTokenType.String
                            && TryParseRaw(reader.GetString()!, out var rv)
                        )
                            raw = rv;
                    }
                }
                return new UsbEndpointAttributes(raw ?? 0);
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, UsbEndpointAttributes value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.Raw);

        private static bool TryParseRaw(string s, out byte v)
        {
            s = s.Trim();
            int paren = s.IndexOf('(');
            if (paren >= 0)
                s = s[(paren + 1)..].TrimEnd(')').Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return byte.TryParse(s[2..], NumberStyles.HexNumber, null, out v);
            return byte.TryParse(s, out v);
        }
    }

    // Hex byte[] converter (unchanged)
    private sealed class HexByteArrayConverter : JsonConverter<byte[]>
    {
        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                return Array.Empty<byte>();
            var s = reader.GetString();
            if (string.IsNullOrEmpty(s))
                return Array.Empty<byte>();
            try
            {
                return Convert.FromBase64String(s);
            }
            catch
            {
                return ParseHex(s!);
            }
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options) =>
            writer.WriteStringValue(ToHex(value));

        private static string ToHex(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
                return "";
            var c = new char[bytes.Length * 2];
            int j = 0;
            foreach (var b in bytes)
            {
                c[j++] = GetHex((b >> 4) & 0xF);
                c[j++] = GetHex(b & 0xF);
            }
            return new string(c);
        }

        private static char GetHex(int v) => (char)(v < 10 ? ('0' + v) : ('A' + (v - 10)));

        private static byte[] ParseHex(string s)
        {
            s = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s[2..] : s;
            if ((s.Length & 1) == 1)
                s = "0" + s;
            var bytes = new byte[s.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((FromHex(s[2 * i]) << 4) | FromHex(s[2 * i + 1]));
            return bytes;
        }

        private static int FromHex(char c) =>
            c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 10,
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => 0,
            };
    }

    // -----------------
    // Utility
    // -----------------
    private static string Indent(this string s, int spaces)
    {
        var pad = new string(' ', spaces);
        var lines = s.Split(separator, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
            lines[i] = pad + lines[i];
        return string.Join(Environment.NewLine, lines);
    }
}

#pragma warning restore CA1305 // Specify IFormatProvider
