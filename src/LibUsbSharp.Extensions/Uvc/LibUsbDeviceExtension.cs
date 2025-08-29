using LibUsbSharp.Transfer;

namespace LibUsbSharp.Extensions.Uvc;

public static class LibUsbDeviceExtension
{
    public static LibUsbResult ControlUvcRead(
        this IUsbDevice device,
        ControlRequestRecipient recipient,
        ControlRequestUvc request,
        ushort value,
        ushort index,
        Span<byte> destination,
        out ushort bytesRead,
        int timeout = Timeout.Infinite
    ) =>
        device.ControlRead(
            recipient,
            ControlRequestType.Class,
            (byte)request,
            value,
            index,
            destination,
            out bytesRead,
            timeout
        );

    public static LibUsbResult ControlUvcWrite(
        this IUsbDevice device,
        ControlRequestRecipient recipient,
        ControlRequestUvc request,
        ushort value,
        ushort index,
        ReadOnlySpan<byte> source,
        out int bytesWritten,
        int timeout = Timeout.Infinite
    ) =>
        device.ControlWrite(
            recipient,
            ControlRequestType.Class,
            (byte)request,
            value,
            index,
            source,
            out bytesWritten,
            timeout
        );
}
