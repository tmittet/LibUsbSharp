using System;

namespace LibUsbSharp.Transfer;

#pragma warning disable CA1720 // Identifier contains type name

// -------- enums --------
public enum UsbDirection : byte { Out = 0, In = 1 }
public enum UsbRequestType : byte { Standard = 0, Class = 1, Vendor = 2 }
public enum UsbRecipient : byte { Device = 0, Interface = 1, Endpoint = 2 }

public enum StandardRequest : byte
{
    GET_STATUS = 0x00, // IN
    CLEAR_FEATURE = 0x01, // OUT
    SET_FEATURE = 0x03, // OUT
    SET_ADDRESS = 0x05, // OUT (device only)
    GET_DESCRIPTOR = 0x06, // IN
    SET_DESCRIPTOR = 0x07, // OUT
    GET_CONFIGURATION = 0x08, // IN (device only)
    SET_CONFIGURATION = 0x09, // OUT (device only)
    GET_INTERFACE = 0x0A, // IN (interface only)
    SET_INTERFACE = 0x0B, // OUT (interface only)
    SYNCH_FRAME = 0x0C  // IN  (endpoint only)
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
    byte bmRequestType,
    byte bRequest,
    ushort wValue,
    ushort wIndex,
    ushort wLength = 0)
{
    public UsbDirection Direction => (UsbDirection)((bmRequestType >> 7) & 0x01);
    public UsbRequestType Type => (UsbRequestType)((bmRequestType >> 5) & 0x03);
    public UsbRecipient Recipient => (UsbRecipient)(bmRequestType & 0x1F);

    public void Deconstruct(out byte bm, out byte br, out ushort v, out ushort i, out ushort l)
    { bm = bmRequestType; br = bRequest; v = wValue; i = wIndex; l = wLength; }

    public SetupPacket ToSetupPacket() => new(bmRequestType, bRequest, wValue, wIndex, wLength);
    public static ControlTransfer FromSetupPacket(SetupPacket sp) =>
        new(sp.BmRequestType, sp.BRequest, sp.WValue, sp.WIndex, sp.WLength);

    public override string ToString() =>
        $"bm=0x{bmRequestType:X2} ({Direction}/{Type}/{Recipient}) bReq=0x{bRequest:X2} val=0x{wValue:X4} idx=0x{wIndex:X4} len={wLength}";

    // ---- DEVICE ----
    public static class Device
    {
        const UsbRecipient Rec = UsbRecipient.Device;

        // STANDARD (enum-only; validated)
        public static class Standard
        {
            public static ControlTransfer In(StandardRequest req, ushort value = 0, ushort index = 0, ushort length = 0)
                => new(BuildBm(UsbDirection.In, UsbRequestType.Standard, Rec), (byte)req, value, index, length);

            public static ControlTransfer Out(StandardRequest req, ushort value = 0, ushort index = 0, ushort length = 0)
                => new(BuildBm(UsbDirection.Out, UsbRequestType.Standard, Rec), (byte)req, value, index, length);
        }

        public static class Class
        {
            public static ControlTransfer In(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.In, UsbRequestType.Class, Rec), bRequest, value, index, length);

            public static ControlTransfer Out(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.Out, UsbRequestType.Class, Rec), bRequest, value, index, length);
        }

        public static class Vendor
        {
            public static ControlTransfer In(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.In, UsbRequestType.Vendor, Rec), bRequest, value, index, length);

            public static ControlTransfer Out(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.Out, UsbRequestType.Vendor, Rec), bRequest, value, index, length);
        }
    }

    // ---- INTERFACE ----
    public static class Interface
    {
        const UsbRecipient Rec = UsbRecipient.Interface;

        public static class Standard
        {
            public static ControlTransfer In(StandardRequest req, ushort value = 0, ushort index = 0, ushort length = 0)
                => new(BuildBm(UsbDirection.In, UsbRequestType.Standard, Rec), (byte)req, value, index, length);

            public static ControlTransfer Out(StandardRequest req, ushort value = 0, ushort index = 0, ushort length = 0)
                => new(BuildBm(UsbDirection.Out, UsbRequestType.Standard, Rec), (byte)req, value, index, length);
        }

        public static class Class
        {
            public static ControlTransfer In(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.In, UsbRequestType.Class, Rec), bRequest, value, index, length);

            public static ControlTransfer Out(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.Out, UsbRequestType.Class, Rec), bRequest, value, index, length);
        }

        public static class Vendor
        {
            public static ControlTransfer In(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.In, UsbRequestType.Vendor, Rec), bRequest, value, index, length);

            public static ControlTransfer Out(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.Out, UsbRequestType.Vendor, Rec), bRequest, value, index, length);
        }
    }


// ---- ENDPOINT ----
public static class Endpoint
    {
        const UsbRecipient Rec = UsbRecipient.Endpoint;

        public static class Standard
        {
            public static ControlTransfer In(StandardRequest req, ushort value = 0, ushort index = 0, ushort length = 0)
                => new(BuildBm(UsbDirection.In, UsbRequestType.Standard, Rec), (byte) req, value, index, length);
            
            public static ControlTransfer Out(StandardRequest req, ushort value = 0, ushort index = 0, ushort length = 0)
                => new(BuildBm(UsbDirection.Out, UsbRequestType.Standard, Rec), (byte)req, value, index, length);
        }

        public static class Class
        {
            public static ControlTransfer In(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.In, UsbRequestType.Class, Rec), bRequest, value, index, length);

            public static ControlTransfer Out(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.Out, UsbRequestType.Class, Rec), bRequest, value, index, length);
        }

        public static class Vendor
        {
            public static ControlTransfer In(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.In, UsbRequestType.Vendor, Rec), bRequest, value, index, length);

            public static ControlTransfer Out(byte bRequest, ushort value, ushort index, ushort length = 0)
                => new(BuildBm(UsbDirection.Out, UsbRequestType.Vendor, Rec), bRequest, value, index, length);
        }
    }
    
    private static byte BuildBm(UsbDirection dir, UsbRequestType type, UsbRecipient rec) =>
        (byte)(((byte)dir << 7) | ((byte)type << 5) | (byte)rec);
}


#pragma warning restore CA1720 // Identifier contains type name
