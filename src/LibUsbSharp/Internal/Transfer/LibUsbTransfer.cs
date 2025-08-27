using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace LibUsbSharp.Internal.Transfer;

internal static class LibUsbTransfer
{
    /// <summary>
    /// Synchronously create, submit and wait for a transfer to complete, be canceled or fail.
    /// NOTE: On macOS, cancelling a transfer may cancel all transfers on specified endpoint.
    /// </summary>
    public static LibUsbResult ExecuteSync(
        ILogger logger,
        nint deviceHandle,
        LibUsbTransferType transferType,
        byte endpointAddress,
        GCHandle bufferHandle,
        int bufferLength,
        uint timeout,
        out int bytesTransferred,
        CancellationToken ct
    )
    {
        bytesTransferred = 0;
        if (ct.IsCancellationRequested)
        {
            return LibUsbResult.Interrupted;
        }

        using var transferCompleteEvent = new ManualResetEvent(false);

        GCHandle callbackHandle = default;
        var transferPtr = IntPtr.Zero;
        var transferStatus = (int)LibUsbTransferStatus.Error;
        var transferLength = 0;
        try
        {
            // Create native callback and pin it so the delegate isn't GC'd in flight
            LibUsbTransferCallback nativeCallback = (ptr) =>
            {
                var transfer = Marshal.PtrToStructure<LibUsbTransferTemplate>(ptr);
                Volatile.Write(ref transferStatus, (int)transfer.Status);
                Volatile.Write(ref transferLength, (int)transfer.ActualLength);
                _ = transferCompleteEvent.Set();
            };
            callbackHandle = GCHandle.Alloc(nativeCallback);

            // Allocate and initialize the libusb transfer
            transferPtr = libusb_alloc_transfer(0);
            // libusb_alloc_transfer returns zero pointer on error
            if (transferPtr == IntPtr.Zero)
            {
                return LibUsbResult.OtherError;
            }
            var transferTemplate = LibUsbTransferTemplate.Create(
                deviceHandle,
                endpointAddress,
                bufferHandle,
                bufferLength,
                transferType,
                timeout,
                nativeCallback
            );
            Marshal.StructureToPtr(transferTemplate, transferPtr, false);

            logger.LogDebug("Submitting transfer {Transfer}", transferTemplate);
            // Submit the USB transfer and then return immediately.
            // The registered LibUsbTransferCallback is invoked on completion.
            var submitResult = (LibUsbResult)libusb_submit_transfer(transferPtr);
            if (submitResult is not LibUsbResult.Success)
            {
                return submitResult;
            }

            // Wait for transfer complete or cancellation. If transfer complete is not signaled;
            // we tell libusb to cancel the transfer and wait for the cancellation to complete.
            if (WaitHandle.WaitAny(new[] { transferCompleteEvent, ct.WaitHandle }) != 0)
            {
                // Tell libusb to cancel the transfer, the final transfer status
                // is received through the LibUsbTransferCallback.
                var cancelResult = (LibUsbResult)libusb_cancel_transfer(transferPtr);
                if (
                    cancelResult is not LibUsbResult.NoDevice and not LibUsbResult.NotFound and not LibUsbResult.Success
                )
                {
                    logger.LogError("Failed to cancel LibUsb transfer. {ErrorMessage}", cancelResult.GetMessage());
                }
                // We should not free the transfer or handle if there is still a chance
                // that the callback is triggered, doing so may result in use-after-free.
                // To avoid this, we wait indefinitely for completion or cancellation.
                // See: https://libusb.sourceforge.io/api-1.0/group__libusb__asyncio.html
                _ = transferCompleteEvent.WaitOne();
            }

            Debug.Assert(
                libusb_cancel_transfer(transferPtr) == (int)LibUsbResult.NotFound,
                "libusb_cancel_transfer should return NotFound, after transfer complete event."
            );

            // The transfer is complete, canceled or failed; map status to result and return
            bytesTransferred = Volatile.Read(ref transferLength);
            return ((LibUsbTransferStatus)Volatile.Read(ref transferStatus)).ToLibUsbError();
        }
        finally
        {
            // Free native transfer and unpin the callback
            if (transferPtr != IntPtr.Zero)
            {
                libusb_free_transfer(transferPtr);
            }
            if (callbackHandle.IsAllocated)
            {
                callbackHandle.Free();
            }
        }
    }

    // LibraryImportAttribute not available in .NET6, silence warning
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'

    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint libusb_alloc_transfer(int isoPackets);

    /// <summary>
    /// Asynchronously cancel a previously submitted transfer. This function returns immediately,
    /// but this does not indicate cancellation is complete.Your callback function will be invoked
    /// at some later time with a transfer status of LIBUSB_TRANSFER_CANCELLED.
    ///
    /// NOTE: This function behaves differently on Darwin-based systems (macOS and iOS):
    /// Calling this function for one transfer will cause all transfers on the same endpoint to be
    /// cancelled. Your callback function will be invoked with a transfer status of
    /// LIBUSB_TRANSFER_CANCELLED for each transfer that was cancelled.
    /// </summary>
    /// <returns>
    /// LIBUSB_ERROR_NOT_FOUND if the transfer is not in progress, already complete, or already
    /// cancelled. A LIBUSB_ERROR code on failure.
    /// </returns>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_cancel_transfer(nint transfer);

    /// <summary>
    /// Free a transfer structure. This should be called for all transfers allocated with
    /// libusb_alloc_transfer(). If the LIBUSB_TRANSFER_FREE_BUFFER flag is set and the transfer
    /// buffer is non-NULL, this function will also free the transfer buffer using the standard
    /// system memory allocator(e.g.free()). It is legal to call this function with a NULL transfer.
    /// In this case, the function will simply return safely. It is not legal to free an active
    /// transfer (one which has been submitted and has not yet completed).
    /// </summary>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_free_transfer(nint transfer);

    /// <summary>
    /// Submit a transfer. This function will fire off the USB transfer and then return immediately.
    /// </summary>
    /// <returns>
    /// 0 on success<br />
    /// LIBUSB_ERROR_NO_DEVICE if the device has been disconnected.<br />
    /// LIBUSB_ERROR_BUSY if the transfer has already been submitted.<br />
    /// LIBUSB_ERROR_NOT_SUPPORTED if the transfer flags are not supported by the OS.<br />
    /// LIBUSB_ERROR_INVALID_PARAM if the transfer size is larger than the OS and/or hardware can
    /// support (see Transfer length limitations) another LIBUSB_ERROR code on other failure.<br />
    /// </returns>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_submit_transfer(nint transfer);

#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'
}
