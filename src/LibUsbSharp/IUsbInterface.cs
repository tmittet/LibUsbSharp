﻿using System.Diagnostics.CodeAnalysis;
using LibUsbSharp.Descriptor;

namespace LibUsbSharp;

public interface IUsbInterface : IDisposable
{
    /// <summary>
    /// The number of this interface.
    /// </summary>
    byte Number { get; }

    /// <summary>
    /// Get an input endpoint, if one exists. If more than one is found, the first is returned.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when UsbInterface is disposed</exception>
    bool TryGetInputEndpoint([NotNullWhen(true)] out IUsbEndpointDescriptor? endpoint);

    /// <summary>
    /// Get an output endpoint, if one exists. If more than one is found, the first is returned.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when UsbInterface is disposed</exception>
    bool TryGetOutputEndpoint([NotNullWhen(true)] out IUsbEndpointDescriptor? endpoint);

    /// <summary>
    /// Bulk read data from the USB device interface. This method blocks until a chunk of data is
    /// received or the optional timeout is reached. Under the hood it submits a libusb transfer,
    /// then waits for the transfer completed, timeout or error callback to be received.
    /// </summary>
    /// <param name="buffer">An output buffer for read bytes</param>
    /// <param name="bytesRead">The number of bytes read</param>
    /// <param name="timeout">An optional timeout for the read operation</param>
    /// <exception cref="ArgumentException">Thrown when timeout is invalid</exception>
    /// <exception cref="ObjectDisposedException">Thrown when UsbInterface is disposed</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no input endpoint is found for this interface.
    /// </exception>
    /// <returns>
    /// Success = The read operation completed successfully.<br />
    /// IO = The read operation failed.<br />
    /// InvalidParameter = Transfer size is larger than OS or hardware can support.<br />
    /// NoDevice = The device has been disconnected.<br />
    /// ResourceBusy = Halt condition detected (endpoint stalled) or control request not supported.<br />
    /// Timeout = The read operation timed out.<br />
    /// Overflow = The device sent more data than requested.<br />
    /// Interrupted = The read operation was cancelled.<br />
    /// </returns>
    LibUsbResult BulkRead(byte[] buffer, out int bytesRead, int timeout = -1);

    /// <summary>
    /// Bulk write data to the USB device interface. This method blocks until a chunk of data has
    /// been written or the timeout is reached. Under the hood it submits a libusb transfer, then
    /// waits for the transfer completed, timeout or error callback to be received.
    /// </summary>
    /// <param name="buffer">A buffer of data to write</param>
    /// <param name="count">The maximum number of bytes to write</param>
    /// <param name="bytesWritten">The number of bytes written</param>
    /// <param name="timeout">A timeout for the write operation</param>
    /// <exception cref="ArgumentException">
    /// Thrown when count is larger than buffer or timeout is invalid
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when UsbInterface is disposed</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no output endpoint is found for this interface.
    /// </exception>
    /// <returns>
    /// Success = The write operation completed successfully.<br />
    /// IO = The write operation failed.<br />
    /// InvalidParameter = Transfer size is larger than OS or hardware can support.<br />
    /// NoDevice = The device has been disconnected.<br />
    /// ResourceBusy = Halt condition detected (endpoint stalled) or control request not supported.<br />
    /// Timeout = The write operation timed out.<br />
    /// Overflow = The host sent more data than expected.<br />
    /// Interrupted = The write operation was cancelled.<br />
    /// NotSupported = The transfer flags are not supported by the operating system.<br />
    /// </returns>
    LibUsbResult BulkWrite(byte[] buffer, int count, out int bytesWritten, int timeout);
}
