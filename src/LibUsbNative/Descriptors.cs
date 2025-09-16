using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("LibUsbNative.Tests")]

namespace LibUsbNative.Descriptors;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable SYSLIB1037
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1720 // Identifier contains type name

// ---------------------------------------------
// New enums replacing previous native (byte) fields
// ---------------------------------------------

public enum UsbDescriptorType : byte
{
    Device = 0x01,
    Configuration = 0x02,

    String = 0x03,
    Interface = 0x04,
    Endpoint = 0x05,
    DeviceQualifier = 0x06,
    OtherSpeedConfiguration = 0x07,
    InterfacePower = 0x08,
    BOS = 0x0F,
    DeviceCapability = 0x10,
    SuperspeedUsbEndpointCompanion = 0x30,
    SuperspeedPlusIsochronousEndpointCompanion = 0x31,
}

[Flags]
public enum UsbConfigAttributes : byte
{
    None = 0,
    RemoteWakeup = 0x20,
    SelfPowered = 0x40,
    MustBeSet = 0x80,
}

public enum UsbClass : byte
{
    PerInterface = 0x00,
    Audio = 0x01,
    Communications = 0x02,
    Hid = 0x03,
    Physical = 0x05,
    Image = 0x06,
    Printer = 0x07,
    MassStorage = 0x08,
    Hub = 0x09,
    CdcData = 0x0A,
    SmartCard = 0x0B,
    ContentSecurity = 0x0D,
    Video = 0x0E,
    PersonalHealthcare = 0x0F,
    AudioVideo = 0x10,
    Billboard = 0x11,
    TypeCBridge = 0x12,
    UsbBulkDisplayProtocol = 0x13,
    MctpOverUsb = 0x14,
    Diagnostic = 0xDC,
    WirelessController = 0xE0,
    Miscellaneous = 0xEF,
    ApplicationSpecific = 0xFE,
    VendorSpecific = 0xFF,
    I3CDevice = 0x3C,
}

public enum UsbEndpointDirection : byte
{
    Out = 0x00,
    In = 0x80,
}

public enum UsbEndpointNumber : byte
{
    Ep0 = 0,
    Ep1 = 1,
    Ep2 = 2,
    Ep3 = 3,
    Ep4 = 4,
    Ep5 = 5,
    Ep6 = 6,
    Ep7 = 7,
    Ep8 = 8,
    Ep9 = 9,
    Ep10 = 10,
    Ep11 = 11,
    Ep12 = 12,
    Ep13 = 13,
    Ep14 = 14,
    Ep15 = 15,
}

public enum UsbEndpointTransferType : byte
{
    Control = 0,
    Isochronous = 1,
    Bulk = 2,
    Interrupt = 3,
}

public enum UsbIsochronousSyncType : byte
{
    NoSynchronization = 0,
    Asynchronous = 1,
    Adaptive = 2,
    Synchronous = 3,
}

public enum UsbIsochronousUsageType : byte
{
    Data = 0,
    Feedback = 1,
    ImplicitFeedbackData = 2,
}

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

/// <summary>
/// Combined representation of bEndpointAddress.
/// </summary>
[JsonConverter(typeof(UsbEndpointAddressFlexibleJsonConverter))]
public readonly struct UsbEndpointAddress
{
    public UsbEndpointNumber Number { get; }
    public UsbEndpointDirection Direction { get; }
    public byte Raw { get; }

    internal UsbEndpointAddress(byte raw)
    {
        Raw = raw;
        Direction = (raw & 0x80) != 0 ? UsbEndpointDirection.In : UsbEndpointDirection.Out;
        Number = (UsbEndpointNumber)(raw & 0x0F);
    }

    public override string ToString() => $"{Direction} {Number} (0x{Raw:X2})";
}

// --------------------------------------------------------------------
// Public interfaces
// --------------------------------------------------------------------
public interface IUsbDeviceDescriptor
{
    byte BLength { get; }
    UsbDescriptorType BDescriptorType { get; }
    ushort BcdUSB { get; }
    UsbClass BDeviceClass { get; }
    byte BDeviceSubClass { get; }
    byte BDeviceProtocol { get; }
    byte BMaxPacketSize0 { get; }
    ushort IdVendor { get; }
    ushort IdProduct { get; }
    ushort BcdDevice { get; }
    byte IManufacturer { get; }
    byte IProduct { get; }
    byte ISerialNumber { get; }
    byte BNumConfigurations { get; }
}

public interface IUsbConfigDescriptor
{
    byte BLength { get; }
    UsbDescriptorType BDescriptorType { get; }
    ushort WTotalLength { get; }
    byte BNumInterfaces { get; }
    byte BConfigurationValue { get; }
    byte IConfiguration { get; }
    UsbConfigAttributes BmAttributes { get; }
    byte MaxPower { get; }
    IReadOnlyList<IUsbInterface> Interfaces { get; }
    byte[] Extra { get; }
}

public interface IUsbInterface
{
    IReadOnlyList<IUsbInterfaceDescriptor> AlternateSettings { get; }
}

public interface IUsbInterfaceDescriptor
{
    byte BLength { get; }
    UsbDescriptorType BDescriptorType { get; }
    byte BInterfaceNumber { get; }
    byte BAlternateSetting { get; }
    byte BNumEndpoints { get; }
    UsbClass BInterfaceClass { get; }
    byte BInterfaceSubClass { get; }
    byte BInterfaceProtocol { get; }
    byte IInterface { get; }
    IReadOnlyList<IUsbEndpointDescriptor> Endpoints { get; }
    byte[] Extra { get; }
}

public interface IUsbEndpointDescriptor
{
    byte BLength { get; }
    UsbDescriptorType BDescriptorType { get; }
    UsbEndpointAddress BEndpointAddress { get; }
    UsbEndpointAttributes BmAttributes { get; }
    ushort WMaxPacketSize { get; }
    byte BInterval { get; }
    byte BRefresh { get; }
    byte BSynchAddress { get; }
    byte[] Extra { get; }
}

// --------------------------------------------------------------------
// Public records
// --------------------------------------------------------------------
public record UsbDeviceDescriptor(
    byte BLength,
    UsbDescriptorType BDescriptorType,
    ushort BcdUSB,
    UsbClass BDeviceClass,
    byte BDeviceSubClass,
    byte BDeviceProtocol,
    byte BMaxPacketSize0,
    ushort IdVendor,
    ushort IdProduct,
    ushort BcdDevice,
    byte IManufacturer,
    byte IProduct,
    byte ISerialNumber,
    byte BNumConfigurations
) : IUsbDeviceDescriptor;

public record UsbConfigDescriptor(
    byte BLength,
    UsbDescriptorType BDescriptorType,
    ushort WTotalLength,
    byte BNumInterfaces,
    byte BConfigurationValue,
    byte IConfiguration,
    UsbConfigAttributes BmAttributes,
    byte MaxPower,
    UsbInterface[] Interfaces,
    byte[] Extra
) : IUsbConfigDescriptor
{
    IReadOnlyList<IUsbInterface> IUsbConfigDescriptor.Interfaces => Array.AsReadOnly(Interfaces);
}

public record UsbInterface(UsbInterfaceDescriptor[] AlternateSettings) : IUsbInterface
{
    IReadOnlyList<IUsbInterfaceDescriptor> IUsbInterface.AlternateSettings => Array.AsReadOnly(AlternateSettings);
}

public record UsbInterfaceDescriptor(
    byte BLength,
    UsbDescriptorType BDescriptorType,
    byte BInterfaceNumber,
    byte BAlternateSetting,
    byte BNumEndpoints,
    UsbClass BInterfaceClass,
    byte BInterfaceSubClass,
    byte BInterfaceProtocol,
    byte IInterface,
    UsbEndpointDescriptor[] Endpoints,
    byte[] Extra
) : IUsbInterfaceDescriptor
{
    IReadOnlyList<IUsbEndpointDescriptor> IUsbInterfaceDescriptor.Endpoints => Array.AsReadOnly(Endpoints);
}

public record UsbEndpointDescriptor(
    byte BLength,
    UsbDescriptorType BDescriptorType,
    UsbEndpointAddress BEndpointAddress,
    UsbEndpointAttributes BmAttributes,
    ushort WMaxPacketSize,
    byte BInterval,
    byte BRefresh,
    byte BSynchAddress,
    byte[] Extra
) : IUsbEndpointDescriptor;

// ---------------------------------------------
// Native mirror structs (unchanged)
// ---------------------------------------------
[StructLayout(LayoutKind.Sequential)]
public struct native_libusb_device_descriptor
{
    public byte bLength;
    public byte bDescriptorType;
    public ushort bcdUSB;
    public byte bDeviceClass;
    public byte bDeviceSubClass;
    public byte bDeviceProtocol;
    public byte bMaxPacketSize0;
    public ushort idVendor;
    public ushort idProduct;
    public ushort bcdDevice;
    public byte iManufacturer;
    public byte iProduct;
    public byte iSerialNumber;
    public byte bNumConfigurations;
}

[StructLayout(LayoutKind.Sequential)]
internal struct native_libusb_config_descriptor
{
    public byte bLength;
    public byte bDescriptorType;
    public ushort wTotalLength;
    public byte bNumInterfaces;
    public byte bConfigurationValue;
    public byte iConfiguration;
    public byte bmAttributes;
    public byte MaxPower;
    public IntPtr interfacePtr;
    public IntPtr extra;
    public int extra_length;
}

[StructLayout(LayoutKind.Sequential)]
internal struct native_libusb_interface
{
    public IntPtr altsetting;
    public int num_altsetting;
}

[StructLayout(LayoutKind.Sequential)]
internal struct native_libusb_interface_descriptor
{
    public byte bLength;
    public byte bDescriptorType;
    public byte bInterfaceNumber;
    public byte bAlternateSetting;
    public byte bNumEndpoints;
    public byte bInterfaceClass;
    public byte bInterfaceSubClass;
    public byte bInterfaceProtocol;
    public byte iInterface;
    public IntPtr endpoint;
    public IntPtr extra;
    public int extra_length;
}

[StructLayout(LayoutKind.Sequential)]
internal struct native_libusb_endpoint_descriptor
{
    public byte bLength;
    public byte bDescriptorType;
    public byte bEndpointAddress;
    public byte bmAttributes;
    public ushort wMaxPacketSize;
    public byte bInterval;
    public byte bRefresh;
    public byte bSynchAddress;
    public IntPtr extra;
    public int extra_length;
}

// -------------------------------------------------
// Conversion helpers
// -------------------------------------------------
internal static class LibusbConfigMarshaler
{
    public static UsbConfigDescriptor FromPointer(IntPtr pConfigDescriptor)
    {
        if (pConfigDescriptor == IntPtr.Zero)
            throw new ArgumentNullException(nameof(pConfigDescriptor));

        var cfg = Marshal.PtrToStructure<native_libusb_config_descriptor>(pConfigDescriptor);

        var interfaces = ReadArray(
            cfg.interfacePtr,
            cfg.bNumInterfaces,
            elemPtr =>
            {
                var nIf = Marshal.PtrToStructure<native_libusb_interface>(elemPtr);

                var alt = ReadArray(
                    nIf.altsetting,
                    nIf.num_altsetting,
                    ifDescPtr =>
                    {
                        var id = Marshal.PtrToStructure<native_libusb_interface_descriptor>(ifDescPtr);

                        var endpoints = ReadArray(
                            id.endpoint,
                            id.bNumEndpoints,
                            epPtr =>
                            {
                                var ep = Marshal.PtrToStructure<native_libusb_endpoint_descriptor>(epPtr);
                                var extraEp = ReadExtra(ep.extra, ep.extra_length);

                                return new UsbEndpointDescriptor(
                                    ep.bLength,
                                    (UsbDescriptorType)ep.bDescriptorType,
                                    new UsbEndpointAddress(ep.bEndpointAddress),
                                    new UsbEndpointAttributes(ep.bmAttributes),
                                    ep.wMaxPacketSize,
                                    ep.bInterval,
                                    ep.bRefresh,
                                    ep.bSynchAddress,
                                    extraEp
                                );
                            },
                            Marshal.SizeOf<native_libusb_endpoint_descriptor>()
                        );

                        var extraIf = ReadExtra(id.extra, id.extra_length);

                        return new UsbInterfaceDescriptor(
                            id.bLength,
                            (UsbDescriptorType)id.bDescriptorType,
                            id.bInterfaceNumber,
                            id.bAlternateSetting,
                            id.bNumEndpoints,
                            (UsbClass)id.bInterfaceClass,
                            id.bInterfaceSubClass,
                            id.bInterfaceProtocol,
                            id.iInterface,
                            endpoints,
                            extraIf
                        );
                    },
                    Marshal.SizeOf<native_libusb_interface_descriptor>()
                );

                return new UsbInterface(alt);
            },
            Marshal.SizeOf<native_libusb_interface>()
        );

        var extraCfg = ReadExtra(cfg.extra, cfg.extra_length);

        return new UsbConfigDescriptor(
            cfg.bLength,
            (UsbDescriptorType)cfg.bDescriptorType,
            cfg.wTotalLength,
            cfg.bNumInterfaces,
            cfg.bConfigurationValue,
            cfg.iConfiguration,
            (UsbConfigAttributes)cfg.bmAttributes,
            cfg.MaxPower,
            interfaces,
            extraCfg
        );
    }

    private static TManaged[] ReadArray<TManaged>(
        IntPtr basePtr,
        int count,
        Func<IntPtr, TManaged> projector,
        int elementSize
    )
    {
        if (count <= 0 || basePtr == IntPtr.Zero)
            return Array.Empty<TManaged>();

        var arr = new TManaged[count];
        for (int i = 0; i < count; i++)
        {
            var elemPtr = IntPtr.Add(basePtr, i * elementSize);
            arr[i] = projector(elemPtr);
        }
        return arr;
    }

    private static byte[] ReadExtra(IntPtr p, int length)
    {
        if (p == IntPtr.Zero || length <= 0)
            return Array.Empty<byte>();
        var bytes = new byte[length];
        Marshal.Copy(p, bytes, 0, length);
        return bytes;
    }
}

// -------------------------------------------------
// Flexible JSON converters (for default JsonSerializer usage)
// These ensure deserialization works even without custom options.
// -------------------------------------------------
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

#pragma warning restore SYSLIB1037
#pragma warning restore CA1720 // Identifier contains type name
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore IDE0079 // Remove unnecessary suppression
