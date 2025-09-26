using LibUsbNative.Enums;

namespace LibUsbSharp.Internal.Transfer;

internal static class LibUsbTransferStatusExtension
{
    public static libusb_error ToLibUsbError(this LibUsbTransferStatus status) =>
        status switch
        {
            LibUsbTransferStatus.Completed => libusb_error.LIBUSB_SUCCESS,
            LibUsbTransferStatus.Error => libusb_error.LIBUSB_ERROR_IO,
            LibUsbTransferStatus.TimedOut => libusb_error.LIBUSB_ERROR_TIMEOUT,
            LibUsbTransferStatus.Canceled => libusb_error.LIBUSB_ERROR_INTERRUPTED,
            LibUsbTransferStatus.Stall => libusb_error.LIBUSB_ERROR_BUSY,
            LibUsbTransferStatus.NoDevice => libusb_error.LIBUSB_ERROR_NO_DEVICE,
            LibUsbTransferStatus.Overflow => libusb_error.LIBUSB_ERROR_OVERFLOW,
        };
}
