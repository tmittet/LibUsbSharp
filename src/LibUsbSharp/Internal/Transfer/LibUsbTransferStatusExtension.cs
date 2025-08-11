namespace LibUsbSharp.Internal.Transfer;

internal static class LibUsbTransferStatusExtension
{
    public static LibUsbResult ToLibUsbError(this LibUsbTransferStatus status) =>
        status switch
        {
            LibUsbTransferStatus.Completed => LibUsbResult.Success,
            LibUsbTransferStatus.Error => LibUsbResult.IoError,
            LibUsbTransferStatus.TimedOut => LibUsbResult.Timeout,
            LibUsbTransferStatus.Cancelled => LibUsbResult.Interrupted,
            LibUsbTransferStatus.Stall => LibUsbResult.ResourceBusy,
            LibUsbTransferStatus.NoDevice => LibUsbResult.NoDevice,
            LibUsbTransferStatus.Overflow => LibUsbResult.Overflow,
        };
}
