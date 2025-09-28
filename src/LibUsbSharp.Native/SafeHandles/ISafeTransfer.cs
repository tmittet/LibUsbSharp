using LibUsbSharp.Native.Enums;

namespace LibUsbSharp.Native.SafeHandles;

public interface ISafeTransfer : IDisposable
{
    /// <summary>
    /// Fire off a USB transfer and then return immediately.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeTransfer is disposed.</exception>
    libusb_error Submit();

    /// <summary>
    /// Asynchronously cancel a previously submitted transfer.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeTransfer is disposed.</exception>
    libusb_error Cancel();

    /// <summary>
    /// Get a pointer to the transfer buffer.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeTransfer is disposed.</exception>
    nint GetBufferPtr();
}
