namespace LibUsbSharp.Extensions.ControlTransfer;

public static class LibUsbDeviceExtension
{
    /// <summary>
    /// Send a ControlRead request. Data is read Device -> Host.
    /// </summary>
    /// <param name="device">A UsbDevice instance.</param>
    /// <param name="transfer">The USB standard spec, class spec or vendor defined request</param>
    /// <param name="destination">A destination span for read bytes</param>
    /// <param name="bytesRead">The number of bytes read</param>
    /// <param name="timeout">Timeout before giving up due to no response being received</param>
    /// <exception cref="ArgumentException">Thrown when the destination buffer is too large.</exception>
    /// <returns>
    /// Success = The read operation completed successfully.<br />
    /// IO = The read operation failed.<br />
    /// InvalidParameter = Transfer size is larger than OS or hardware can support.<br />
    /// NoDevice = The device has been disconnected.<br />
    /// ResourceBusy = Halt condition detected (endpoint stalled) or control request not supported.<br />
    /// Timeout = The read operation timed out.<br />
    /// Overflow = The device sent more data than expected.<br />
    /// Interrupted = The read operation was canceled.<br />
    /// NotSupported = The transfer flags are not supported by the operating system.<br />
    /// </returns>
    public static LibUsbResult ControlRead(
        this IUsbDevice device,
        ControlRequest transfer,
        Span<byte> destination,
        out ushort bytesRead,
        int timeout = Timeout.Infinite
    ) =>
        device.ControlRead(
            transfer.Recipient,
            transfer.Type,
            transfer.Request,
            transfer.Value,
            transfer.Index,
            destination,
            out bytesRead,
            timeout
        );

    /// <summary>
    /// Send a ControlWrite request. Data is written Host -> Device.
    /// </summary>
    /// <param name="device">A UsbDevice instance.</param>
    /// <param name="transfer">The USB standard spec, class spec or vendor defined request</param>
    /// <param name="source">The payload to send to the device (max. 65.535 bytes)</param>
    /// <param name="timeout">Timeout before giving up due to no response being received</param>
    /// <param name="bytesWritten">The actual number of bytes written to the device</param>
    /// <exception cref="ArgumentException">Thrown when the source payload is too large.</exception>
    /// <returns>
    /// Success = The write operation completed successfully.<br />
    /// IO = The write operation failed.<br />
    /// InvalidParameter = Transfer size is larger than OS or hardware can support.<br />
    /// NoDevice = The device has been disconnected.<br />
    /// ResourceBusy = Halt condition detected (endpoint stalled) or control request not supported.<br />
    /// Timeout = The write operation timed out.<br />
    /// Overflow = The host sent more data than expected.<br />
    /// Interrupted = The write operation was canceled.<br />
    /// NotSupported = The transfer flags are not supported by the operating system.<br />
    /// </returns>
    public static LibUsbResult ControlWrite(
        this IUsbDevice device,
        ControlRequest transfer,
        ReadOnlySpan<byte> source,
        out int bytesWritten,
        int timeout = Timeout.Infinite
    ) =>
        device.ControlWrite(
            transfer.Recipient,
            transfer.Type,
            transfer.Request,
            transfer.Value,
            transfer.Index,
            source,
            out bytesWritten,
            timeout
        );
}
