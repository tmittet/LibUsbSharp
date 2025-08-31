using RequestRecipient = LibUsbSharp.Transfer.ControlRequestRecipient;
using RequestType = LibUsbSharp.Transfer.ControlRequestType;

namespace LibUsbSharp.Extensions.ControlTransfer.Uvc;

/// <summary>
/// A UVC control requst setup packet factory.
/// </summary>
public static class ControlRequestUvc
{
    public static class Interface
    {
        public static ControlRequest Class(
            UvcRequest request,
            byte interfaceNumber,
            byte processingUnit,
            ushort value = 0,
            ushort length = 0
        ) =>
            new(
                RequestRecipient.Interface,
                RequestType.Class,
                (byte)request,
                value,
                (ushort)(processingUnit << 8 | interfaceNumber),
                length
            );
    }
}
