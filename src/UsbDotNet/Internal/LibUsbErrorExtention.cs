using LibUsbSharp.Native.Enums;

namespace LibUsbSharp.Internal;

internal static class LibUsbErrorExtention
{
    internal static LibUsbResult ToLibUsbResult(this libusb_error libusbError) =>
        libusbError switch
        {
            libusb_error.LIBUSB_SUCCESS => LibUsbResult.Success,
            libusb_error.LIBUSB_ERROR_IO => LibUsbResult.IoError,
            libusb_error.LIBUSB_ERROR_INVALID_PARAM => LibUsbResult.InvalidParameter,
            libusb_error.LIBUSB_ERROR_ACCESS => LibUsbResult.AccessDenied,
            libusb_error.LIBUSB_ERROR_NO_DEVICE => LibUsbResult.NoDevice,
            libusb_error.LIBUSB_ERROR_NOT_FOUND => LibUsbResult.NotFound,
            libusb_error.LIBUSB_ERROR_BUSY => LibUsbResult.ResourceBusy,
            libusb_error.LIBUSB_ERROR_TIMEOUT => LibUsbResult.Timeout,
            libusb_error.LIBUSB_ERROR_OVERFLOW => LibUsbResult.Overflow,
            libusb_error.LIBUSB_ERROR_PIPE => LibUsbResult.PipeError,
            libusb_error.LIBUSB_ERROR_INTERRUPTED => LibUsbResult.Interrupted,
            libusb_error.LIBUSB_ERROR_NO_MEM => LibUsbResult.InsufficientMemory,
            libusb_error.LIBUSB_ERROR_NOT_SUPPORTED => LibUsbResult.NotSupported,
            libusb_error.LIBUSB_ERROR_OTHER => LibUsbResult.OtherError,
        };
}
