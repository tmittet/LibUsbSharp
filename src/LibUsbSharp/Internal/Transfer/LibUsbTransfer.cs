using System.Runtime.InteropServices;

namespace LibUsbSharp.Internal.Transfer;

internal static class LibUsbTransfer
{
    /// <summary>
    /// Synchronously create, submit and wait for a transfer to complete, be canceled or fail.
    /// </summary>
    public static LibUsbResult ExecuteSync(
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
        var transferStatus = LibUsbTransferStatus.Error;
        var transferLength = 0;
        try
        {
            // Create native callback and pin it so the delegate isn't GC'd in flight
            LibUsbTransferCallback nativeCallback = (ptr) =>
            {
                var transfer = Marshal.PtrToStructure<LibUsbTransferTemplate>(ptr);
                transferStatus = transfer.Status;
                transferLength = transfer.ActualLength;
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
                // Tell libusb to cancel the transfer and discard the returned cancel status,
                // the final transfer status is received through the LibUsbTransferCallback.
                _ = libusb_cancel_transfer(transferPtr);
                // We should not free the transfer or handle if there is still a chance
                // that the callback is triggered, doing so may result in use-after-free.
                // To avoid this, we wait indefinitely for completion or cancellation.
                _ = transferCompleteEvent.WaitOne();
            }

            // The transfer is complete, canceled or failed; map status to result and return
            bytesTransferred = transferLength;
            return transferStatus.ToLibUsbError();
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

    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_cancel_transfer(nint transfer);

    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_free_transfer(nint transfer);

    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_submit_transfer(nint transfer);

#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'
}
