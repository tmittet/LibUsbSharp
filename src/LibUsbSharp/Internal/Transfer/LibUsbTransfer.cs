using System.Diagnostics;
using System.Runtime.InteropServices;
using LibUsbNative.SafeHandles;
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
        ISafeDeviceHandle deviceHandle,
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
            using var transferBuffer = ISafeTransfer.Allocate(0);
            transferPtr = transferBuffer.GetBufferPtr();

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

#if DEBUG
            logger.LogTrace("Submitting transfer: {Transfer}.", transferTemplate);
#endif
            // Submit the USB transfer and then return immediately.
            // The registered LibUsbTransferCallback is invoked on completion.
            var submitResult = (LibUsbResult)transferBuffer.Submit();
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
                var cancelResult = (LibUsbResult)transferBuffer.Cancel();
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
                (int)transferBuffer.Cancel() == (int)LibUsbResult.NotFound,
                "libusb_cancel_transfer should return NotFound, after transfer complete event."
            );

            // The transfer is complete, canceled or failed; map status to result and return
            bytesTransferred = Volatile.Read(ref transferLength);
            return ((LibUsbTransferStatus)Volatile.Read(ref transferStatus)).ToLibUsbError();
        }
        finally
        {
            if (callbackHandle.IsAllocated)
            {
                callbackHandle.Free();
            }
        }
    }
}
