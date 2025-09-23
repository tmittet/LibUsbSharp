using System.Globalization;
using System.Text;
using LibUsbNative.Descriptors;

namespace LibUsbNative.Extensions;

// -----------------------
// Tree/structured printing (unchanged – still rich / human readable)
// -----------------------
public static class DescriptorToStringExtension
{
    private static readonly CultureInfo _culture = CultureInfo.InvariantCulture;
    private static readonly string[] _newLine = new[] { "\r\n", "\n" };

    public static string ToTreeString(this UsbDeviceDescriptor d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Device Descriptor:");
        sb.AppendLine(_culture, $"  bLength              : {d.BLength}");
        sb.AppendLine(_culture, $"  bDescriptorType      : {Fmt(d.BDescriptorType)}");
        sb.AppendLine(_culture, $"  bcdUSB               : 0x{d.BcdUSB:X4}");
        sb.AppendLine(_culture, $"  bDeviceClass         : {Fmt(d.BDeviceClass)}");
        sb.AppendLine(_culture, $"  bDeviceSubClass      : 0x{d.BDeviceSubClass:X2}");
        sb.AppendLine(_culture, $"  bDeviceProtocol      : 0x{d.BDeviceProtocol:X2}");
        sb.AppendLine(_culture, $"  bMaxPacketSize0      : {d.BMaxPacketSize0}");
        sb.AppendLine(_culture, $"  idVendor             : 0x{d.IdVendor:X4}");
        sb.AppendLine(_culture, $"  idProduct            : 0x{d.IdProduct:X4}");
        sb.AppendLine(_culture, $"  bcdDevice            : 0x{d.BcdDevice:X4}");
        sb.AppendLine(_culture, $"  iManufacturer        : {d.IManufacturer}");
        sb.AppendLine(_culture, $"  iProduct             : {d.IProduct}");
        sb.AppendLine(_culture, $"  iSerialNumber        : {d.ISerialNumber}");
        sb.AppendLine(_culture, $"  bNumConfigurations   : {d.BNumConfigurations}");
        return sb.ToString().TrimEnd();
    }

    public static string ToTreeString(this UsbDeviceDescriptor d, IReadOnlyList<UsbConfigDescriptor> configs)
    {
        var sb = new StringBuilder();
        sb.AppendLine(d.ToTreeString());
        for (var i = 0; i < configs.Count; i++)
        {
            sb.AppendLine();
            sb.Append(configs[i].ToTreeString().Indent(2));
        }
        return sb.ToString().TrimEnd();
    }

    public static string ToTreeString(this UsbConfigDescriptor cfg)
    {
        var sb = new StringBuilder()
            .AppendLine("Configuration Descriptor:")
            .AppendLine(_culture, $"  bLength             : {cfg.BLength}")
            .AppendLine(_culture, $"  bDescriptorType     : {Fmt(cfg.BDescriptorType)}")
            .AppendLine(_culture, $"  wTotalLength        : {cfg.WTotalLength}")
            .AppendLine(_culture, $"  bNumInterfaces      : {cfg.BNumInterfaces}")
            .AppendLine(_culture, $"  bConfigurationValue : {cfg.BConfigurationValue}")
            .AppendLine(_culture, $"  iConfiguration      : {cfg.IConfiguration}")
            .AppendLine(_culture, $"  bmAttributes        : {Fmt(cfg.BmAttributes)}")
            .AppendLine(_culture, $"  MaxPower            : {cfg.MaxPower} (units of 2mA)");
        if (cfg.Extra is { Length: > 0 })
        {
            sb.AppendLine(_culture, $"  Extra               : {cfg.Extra.Length} bytes");
        }
        for (var i = 0; i < cfg.Interfaces.Count; i++)
        {
            var iface = cfg.Interfaces[i];
            sb.AppendLine();
            sb.AppendLine(_culture, $"  Interface[{i}]:");
            for (var a = 0; a < iface.AlternateSettings.Count; a++)
            {
                var alt = iface.AlternateSettings[a];
                sb.Append(alt.ToTreeString().Indent(4));
            }
        }
        return sb.ToString().TrimEnd();
    }

    public static string ToTreeString(this libusb_interface_descriptor id)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Interface Descriptor:");
        sb.AppendLine(_culture, $"  bLength           : {id.bLength}");
        sb.AppendLine(_culture, $"  bDescriptorType   : {Fmt(id.bDescriptorType)}");
        sb.AppendLine(_culture, $"  bInterfaceNumber  : {id.bInterfaceNumber}");
        sb.AppendLine(_culture, $"  bAlternateSetting : {id.bAlternateSetting}");
        sb.AppendLine(_culture, $"  bNumEndpoints     : {id.bNumEndpoints}");
        sb.AppendLine(_culture, $"  bInterfaceClass   : {Fmt(id.bInterfaceClass)}");
        sb.AppendLine(_culture, $"  bInterfaceSubClass: 0x{id.bInterfaceSubClass:X2}");
        sb.AppendLine(_culture, $"  bInterfaceProtocol: 0x{id.bInterfaceProtocol:X2}");
        sb.AppendLine(_culture, $"  iInterface        : {id.iInterface}");
        if (id.extra is { Length: > 0 })
            sb.AppendLine(_culture, $"  Extra             : {id.extra.Length} bytes");
        foreach (var ep in id.endpoints)
        {
            sb.AppendLine();
            sb.Append(ep.ToTreeString().Indent(2));
        }
        return sb.ToString();
    }

    public static string ToTreeString(this UsbEndpointDescriptor ep)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Endpoint Descriptor:");
        sb.AppendLine(_culture, $"  bLength          : {ep.BLength}");
        sb.AppendLine(_culture, $"  bDescriptorType  : {Fmt(ep.BDescriptorType)}");
        sb.AppendLine(
            _culture,
            $"  bEndpointAddress : 0x{ep.BEndpointAddress.Raw:X2} ({ep.BEndpointAddress.Direction}, {ep.BEndpointAddress.Number})"
        );
        sb.AppendLine(
            _culture,
            $"  bmAttributes     : 0x{ep.BmAttributes.Raw:X2} ({ep.BmAttributes.TransferType}/{ep.BmAttributes.SyncType}/{ep.BmAttributes.UsageType})"
        );
        sb.AppendLine(_culture, $"  wMaxPacketSize   : {ep.WMaxPacketSize}");
        sb.AppendLine(_culture, $"  bInterval        : {ep.BInterval}");
        sb.AppendLine(_culture, $"  bRefresh         : {ep.BRefresh}");
        sb.AppendLine(_culture, $"  bSynchAddress    : {ep.BSynchAddress}");
        if (ep.Extra is { Length: > 0 })
            sb.AppendLine(_culture, $"  Extra            : {ep.Extra.Length} bytes");
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Enum formatting helper (for tree output)
    /// </summary>
    private static string Fmt<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var raw = Convert.ToUInt64(value, _culture);
        var rawStr =
            raw <= 0xFF ? $"0x{raw:X2}"
            : raw <= 0xFFFF ? $"0x{raw:X4}"
            : $"0x{raw:X}";
        return $"{value} ({rawStr})";
    }

    /// <summary>
    /// Indent lines helper (for tree output)
    /// </summary>
    private static string Indent(this string s, int spaces)
    {
        var pad = new string(' ', spaces);
        var lines = s.Split(_newLine, StringSplitOptions.None);
        for (var i = 0; i < lines.Length; i++)
            lines[i] = pad + lines[i];
        return string.Join(Environment.NewLine, lines);
    }
}
