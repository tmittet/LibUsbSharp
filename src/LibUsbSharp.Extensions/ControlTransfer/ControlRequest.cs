using RequestRecipient = LibUsbSharp.Transfer.ControlRequestRecipient;
using RequestType = LibUsbSharp.Transfer.ControlRequestType;

namespace LibUsbSharp.Extensions.ControlTransfer;

/// <summary>
/// A control requst setup packet factory.
/// </summary>
public sealed record ControlRequest(
    RequestRecipient Recipient,
    RequestType Type,
    byte Request,
    ushort Value,
    ushort Index,
    ushort Length = 0
)
{
    /// <summary>
    /// Requests directed to the device.
    /// </summary>
    public static class Device
    {
        public static ControlRequest Standard(
            StandardRequest request,
            ushort value = 0,
            ushort index = 0,
            ushort length = 0
        ) => new(RequestRecipient.Device, RequestType.Standard, (byte)request, value, index, length);

        public static ControlRequest Class(byte request, ushort value, ushort index, ushort length = 0) =>
            new(RequestRecipient.Device, RequestType.Class, request, value, index, length);

        public static ControlRequest Vendor(byte request, ushort value, ushort index, ushort length = 0) =>
            new(RequestRecipient.Device, RequestType.Vendor, request, value, index, length);
    }

    /// <summary>
    /// Requests directed to the device interface.
    /// </summary>
    public static class Interface
    {
        public static ControlRequest Standard(
            StandardRequest request,
            ushort value = 0,
            ushort index = 0,
            ushort length = 0
        ) => new(RequestRecipient.Interface, RequestType.Standard, (byte)request, value, index, length);

        public static ControlRequest Class(byte request, ushort value, ushort index, ushort length = 0) =>
            new(RequestRecipient.Interface, RequestType.Class, request, value, index, length);

        public static ControlRequest Vendor(byte request, ushort value, ushort index, ushort length = 0) =>
            new(RequestRecipient.Interface, RequestType.Vendor, request, value, index, length);
    }

    /// <summary>
    /// Requests directed to a specific endpoint on a device.
    /// </summary>
    public static class Endpoint
    {
        public static ControlRequest Standard(
            StandardRequest request,
            ushort value = 0,
            ushort index = 0,
            ushort length = 0
        ) => new(RequestRecipient.Endpoint, RequestType.Standard, (byte)request, value, index, length);

        public static ControlRequest Class(byte request, ushort value, ushort index, ushort length = 0) =>
            new(RequestRecipient.Endpoint, RequestType.Class, request, value, index, length);

        public static ControlRequest Vendor(byte request, ushort value, ushort index, ushort length = 0) =>
            new(RequestRecipient.Endpoint, RequestType.Vendor, request, value, index, length);
    }

    /// <summary>
    /// Requests directed to "other" elements defined by the class or vendor spec.
    /// </summary>
    public static class Other
    {
        public static ControlRequest Class(byte request, ushort value, ushort index, ushort length = 0) =>
            new(RequestRecipient.Other, RequestType.Class, request, value, index, length);

        public static ControlRequest Vendor(byte request, ushort value, ushort index, ushort length = 0) =>
            new(RequestRecipient.Other, RequestType.Vendor, request, value, index, length);
    }

    public override string ToString() =>
        $"{Recipient}/{Type} (0x{(byte)Type << 5 | (byte)Recipient:X2}), "
        + $"request=0x{Request:X2}, value=0x{Value:X4}, index=0x{Index:X4}, length={Length}";
}
