using System;

#pragma warning disable CA1720 // Identifier contains type name

namespace LibUsbSharp.Transfer;

// UVC (USB Video Class) request codes.
// High bit (0x80) in these request codes indicates an IN (GET_*) request as per UVC spec.
public enum UvcRequest : byte
{
    SetCur = 0x01,
    SetCurAll = 0x11,
    GetCur = 0x81,
    GetMin = 0x82,
    GetMax = 0x83,
    GetRes = 0x84,
    GetLen = 0x85,
    GetInfo = 0x86,
    GetDef = 0x87,
    GetCurAll = 0x91,
}

// Strongly typed UVC control request descriptor that can be converted to a generic ControlTransfer.
public readonly record struct UvcControlRequest(UvcRequest Request, ushort Value, ushort Index, ushort Length = 0)
{
    internal ControlTransfer ToControlTransfer(UsbRecipient recipient)
    {
        var direction = UvcHelpers.IsIn(Request) ? UsbDirection.In : UsbDirection.Out;
        return new ControlTransfer(recipient, UsbRequestType.Class, direction, (byte)Request, Value, Index, Length);
    }
}

internal static class UvcHelpers
{
    internal static bool IsIn(UvcRequest req) => ((byte)req & 0x80) != 0;

    internal static void ValidateDirection(UvcRequest req, UsbDirection dir)
    {
        var isIn = IsIn(req);
        if (isIn && dir != UsbDirection.In)
            throw new ArgumentException($"Request {req} is an IN (GET_*) request.");
        if (!isIn && dir != UsbDirection.Out)
            throw new ArgumentException($"Request {req} is an OUT (SET_*) request.");
    }
}

// Partial extension adding UVC helpers under ControlTransfer.*.Class.Uvc
public sealed partial record ControlTransfer
{
    public static partial class Interface
    {
        public static partial class Class
        {
            public static class Uvc
            {
                public static ControlTransfer In(UvcRequest request, ushort value, ushort index, ushort length = 0)
                {
                    UvcHelpers.ValidateDirection(request, UsbDirection.In);
                    return new ControlTransfer(
                        UsbRecipient.Interface,
                        UsbRequestType.Class,
                        UsbDirection.In,
                        (byte)request,
                        value,
                        index,
                        length
                    );
                }

                public static ControlTransfer Out(UvcRequest request, ushort value, ushort index, ushort length = 0)
                {
                    UvcHelpers.ValidateDirection(request, UsbDirection.Out);
                    return new ControlTransfer(
                        UsbRecipient.Interface,
                        UsbRequestType.Class,
                        UsbDirection.Out,
                        (byte)request,
                        value,
                        index,
                        length
                    );
                }

                public static ControlTransfer From(UvcControlRequest request) =>
                    request.ToControlTransfer(UsbRecipient.Interface);
            }
        }
    }

    public static partial class Endpoint
    {
        public static partial class Class
        {
            public static class Uvc
            {
                public static ControlTransfer In(UvcRequest request, ushort value, ushort index, ushort length = 0)
                {
                    UvcHelpers.ValidateDirection(request, UsbDirection.In);
                    return new ControlTransfer(
                        UsbRecipient.Endpoint,
                        UsbRequestType.Class,
                        UsbDirection.In,
                        (byte)request,
                        value,
                        index,
                        length
                    );
                }

                public static ControlTransfer Out(UvcRequest request, ushort value, ushort index, ushort length = 0)
                {
                    UvcHelpers.ValidateDirection(request, UsbDirection.Out);
                    return new ControlTransfer(
                        UsbRecipient.Endpoint,
                        UsbRequestType.Class,
                        UsbDirection.Out,
                        (byte)request,
                        value,
                        index,
                        length
                    );
                }

                public static ControlTransfer From(UvcControlRequest request) =>
                    request.ToControlTransfer(UsbRecipient.Endpoint);
            }
        }
    }
}

#pragma warning restore CA1720 // Identifier contains type name
