using System;
using System.Net;
using System.Reflection;

namespace LibUsbSharp.Transfer;

#pragma warning disable CA1720 // Identifier contains type name

// -------- enums --------
public enum UsbDirection : byte
{
    Out = 0,
    In = 1,
}

public enum UsbRequestType : byte
{
    Standard = 0,
    Class = 1,
    Vendor = 2,
}

public enum UsbRecipient : byte
{
    Device = 0,
    Interface = 1,
    Endpoint = 2,
}

public enum StandardRequest : byte
{
    GetStatus = 0x00, // IN
    ClearFeature = 0x01, // OUT
    SetFeature = 0x03, // OUT
    SetAddress = 0x05, // OUT (device only)
    GetDescriptor = 0x06, // IN
    SetDescriptor = 0x07, // OUT
    GetConfiguration = 0x08, // IN (device only)
    SetConfiguration = 0x09, // OUT (device only)
    GetInterface = 0x0A, // IN (interface only)
    SetInterface = 0x0B, // OUT (interface only)
    SyncFrame = 0x0C // IN  (endpoint only)
}

public enum DescriptorType : byte
{
    Device = 1, Configuration = 2, String = 3, Interface = 4, Endpoint = 5,
    DeviceQualifier = 6, OtherSpeed = 7, InterfacePower = 8, BOS = 15
}

// -------- setup packet (8 bytes) --------
public readonly struct SetupPacket
{
    private readonly byte bRequest;
    private readonly ushort wLength;
    private readonly byte bmRequestType;
    private readonly ushort wValue;
    private readonly ushort wIndex;

    public SetupPacket(byte bmRequestType, byte bRequest, ushort wValue, ushort wIndex, ushort wLength)
    { this.bmRequestType = bmRequestType; this.bRequest = bRequest; this.wValue = wValue; this.wIndex = wIndex; this.wLength = wLength; }

    public byte BmRequestType => bmRequestType;

    public byte BRequest => bRequest;

    public ushort WValue => wValue;

    public ushort WIndex => wIndex;

    public ushort WLength => wLength;

    public byte[] ToBytes()
    {
        var buf = new byte[8];
        buf[0] = BmRequestType; buf[1] = BRequest;
        buf[2] = (byte)(WValue & 0xFF); buf[3] = (byte)(WValue >> 8);
        buf[4] = (byte)(WIndex & 0xFF); buf[5] = (byte)(WIndex >> 8);
        buf[6] = (byte)(WLength & 0xFF); buf[7] = (byte)(WLength >> 8);
        return buf;
    }
}

// -------- generic control transfer --------
public sealed record ControlTransfer(
    UsbRecipient Recipient,
    UsbRequestType Type,
    UsbDirection Direction,
    byte Request,
    ushort Value,
    ushort Index,
    ushort Length = 0
)
{
    public byte[] ToSetupPacket()
    {
        var buf = new byte[8];
        var BmRequestType = (byte)(((byte)Direction << 7) | ((byte)Type << 5) | (byte)Recipient);
        buf[0] = BmRequestType;
        buf[1] = Request;
        buf[2] = (byte)(Value & 0xFF);
        buf[3] = (byte)(Value >> 8);
        buf[4] = (byte)(Index & 0xFF);
        buf[5] = (byte)(Index >> 8);
        buf[6] = (byte)(Length & 0xFF);
        buf[7] = (byte)(Length >> 8);
        return buf;
    }

    public override string ToString() =>
        $"{Recipient}/{Type}/{Direction}) bReq=0x{Request:X2} val=0x{Value:X4} idx=0x{Index:X4} len={Length}";

    // ---- DEVICE ----
    public static class Device
    {
        private const UsbRecipient Recipient = UsbRecipient.Device;

        // STANDARD (enum-only; validated)
        public static class Standard
        {
            public static ControlTransfer In(
                StandardRequest req,
                ushort value = 0,
                ushort index = 0,
                ushort length = 0
            ) =>
                new(
                    UsbRecipient.Device,
                    UsbRequestType.Standard,
                    UsbDirection.In,
                    (byte)req,
                    value,
                    index,
                    length
                );

            public static ControlTransfer Out(
                StandardRequest req,
                ushort value = 0,
                ushort index = 0,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Standard,
                    UsbDirection.Out,
                    (byte)req,
                    value,
                    index,
                    length
                );
        }

        public static class Class
        {
            public static ControlTransfer In(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Class,
                    UsbDirection.In,
                    bRequest,
                    value,
                    index,
                    length
                );

            public static ControlTransfer Out(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Class,
                    UsbDirection.Out,
                    bRequest,
                    value,
                    index,
                    length
                );
        }

        public static class Vendor
        {
            public static ControlTransfer In(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Vendor,
                    UsbDirection.In,
                    bRequest,
                    value,
                    index,
                    length
                );

            public static ControlTransfer Out(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Vendor,
                    UsbDirection.Out,
                    bRequest,
                    value,
                    index,
                    length
                );
        }
    }

    // ---- INTERFACE ----
    public static class Interface
    {
        private const UsbRecipient Recipient = UsbRecipient.Interface;

        public static class Standard
        {
            public static ControlTransfer In(
                StandardRequest req,
                ushort value = 0,
                ushort index = 0,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Standard,
                    UsbDirection.In,
                    (byte)req,
                    value,
                    index,
                    length
                );

            public static ControlTransfer Out(
                StandardRequest req,
                ushort value = 0,
                ushort index = 0,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Standard,
                    UsbDirection.Out,
                    (byte)req,
                    value,
                    index,
                    length
                );
        }

        public static class Class
        {
            public static ControlTransfer In(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Class,
                    UsbDirection.In,
                    bRequest,
                    value,
                    index,
                    length
                );

            public static ControlTransfer Out(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Class,
                    UsbDirection.Out,
                    bRequest,
                    value,
                    index,
                    length
                );
        }

        public static class Vendor
        {
            public static ControlTransfer In(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Vendor,
                    UsbDirection.In,
                    bRequest,
                    value,
                    index,
                    length
                );

            public static ControlTransfer Out(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Vendor,
                    UsbDirection.Out,
                    bRequest,
                    value,
                    index,
                    length
                );
        }
    }

    public static class Endpoint
    {
        private const UsbRecipient Recipient = UsbRecipient.Endpoint;

        public static class Standard
        {
            public static ControlTransfer In(
                StandardRequest req,
                ushort value = 0,
                ushort index = 0,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Standard,
                    UsbDirection.In,
                    (byte)req,
                    value,
                    index,
                    length
                );

            public static ControlTransfer Out(
                StandardRequest req,
                ushort value = 0,
                ushort index = 0,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Standard,
                    UsbDirection.Out,
                    (byte)req,
                    value,
                    index,
                    length
                );
        }

        public static class Class
        {
            public static ControlTransfer In(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Class,
                    UsbDirection.In,
                    bRequest,
                    value,
                    index,
                    length
                );

            public static ControlTransfer Out(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Class,
                    UsbDirection.Out,
                    bRequest,
                    value,
                    index,
                    length
                );
        }

        public static class Vendor
        {
            public static ControlTransfer In(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Vendor,
                    UsbDirection.In,
                    bRequest,
                    value,
                    index,
                    length
                );

            public static ControlTransfer Out(
                byte bRequest,
                ushort value,
                ushort index,
                ushort length = 0
            ) =>
                new(
                    Recipient,
                    UsbRequestType.Vendor,
                    UsbDirection.Out,
                    bRequest,
                    value,
                    index,
                    length
                );
        }
    }
}


#pragma warning restore CA1720 // Identifier contains type name
