using System.Runtime.InteropServices;

namespace LibUsbSharp.Internal.Transfer;

internal sealed class LibUsbTransfer : IDisposable
{
    private readonly nint _transferPtr;
    private readonly GCHandle _callbackHandle;
    private readonly object _disposeLock = new();
    private bool _disposed;

    internal delegate void TransferCompletedHandler(
        LibUsbTransfer transfer,
        LibUsbTransferStatus status,
        int actualLength
    );

    private readonly TransferCompletedHandler _onTransferComplete;

    /// <summary>
    /// Creates a LibUsb transfer that can be submitted to LibUsb. After the transfer has completed,
    /// the LibUsb library populates the transfer with the results and passes it back to the user.
    /// </summary>
    /// <param name="deviceHandle">
    /// Handle of the device that this transfer will be submitted to.
    /// </param>
    /// <param name="endpoint">
    /// Address of the endpoint where this transfer will be sent.
    /// </param>
    /// <param name="bufferHandle"></param>
    /// <param name="bufferLength"></param>
    /// <param name="type"></param>
    /// <param name="timeout">
    /// A timeout of 0 means no timeout. The transfer will wait
    /// indefinitely until it completes, fails, or is cancelled.
    /// </param>
    /// <param name="completedHandler">Called on transfer completion.</param>
    internal LibUsbTransfer(
        nint deviceHandle,
        byte endpoint,
        GCHandle bufferHandle,
        int bufferLength,
        LibUsbTransferType type,
        uint timeout,
        TransferCompletedHandler completedHandler
    )
    {
        _onTransferComplete =
            completedHandler ?? throw new ArgumentNullException(nameof(completedHandler));

        // Allocate the LibUsb transfer
        _transferPtr = libusb_alloc_transfer(0);
        if (_transferPtr == IntPtr.Zero)
        {
            throw LibUsbException.FromResult(
                LibUsbResult.OtherError,
                "Failed to allocate transfer."
            );
        }

        // Set up a handler for the native callback and create a handle for it
        LibUsbTransferCallback nativeCallback = (transferPtr) =>
        {
            var transfer = Marshal.PtrToStructure<LibUsbTransferTemplate>(transferPtr);
            _onTransferComplete(this, transfer.Status, transfer.ActualLength);
        };
        _callbackHandle = GCHandle.Alloc(nativeCallback);

        // Create the LibUsbTransfer struct and marshal data to the allocated LibUsb transfer
        var transfer = new LibUsbTransferTemplate
        {
            DeviceHandle = deviceHandle,
            Flags = 0,
            Endpoint = endpoint,
            Type = type,
            Timeout = timeout,
            Status = LibUsbTransferStatus.Completed,
            Length = bufferLength,
            ActualLength = 0,
            Callback = nativeCallback,
            UserData = IntPtr.Zero,
            Buffer = bufferHandle.AddrOfPinnedObject(),
            NumIsoPackets = 0,
        };
        Marshal.StructureToPtr(transfer, _transferPtr, false);
    }

    /// <summary>
    /// Submit the USB transfer and then return immediately.
    /// The registered TransferCompletedHandler is invoked on completion.
    /// </summary>
    /// <returns>
    /// Success = Transfer successfully submitted.<br />
    /// NotFound = The LibUsbTransfer object is disposed.<br />
    /// NoDevice = The device has been disconnected.<br />
    /// ResourceBusy = The transfer has already been submitted.<br />
    /// NotSupported = The transfer flags are not supported by the operating system.<br />
    /// InvalidParameter = The transfer size is larger than OS or hardware can support.<br />
    /// </returns>
    public LibUsbResult Submit()
    {
        lock (_disposeLock)
        {
            return _disposed
                ? LibUsbResult.NotFound
                : (LibUsbResult)libusb_submit_transfer(_transferPtr);
        }
    }

    /// <summary>
    /// Attempts to cancel a transfer. Returns NotFound when attempting to cancel
    /// an unsubmitted transfer or cancelling a disposed LibUsbTransfer object.
    /// </summary>
    public LibUsbResult Cancel()
    {
        lock (_disposeLock)
        {
            return _disposed
                ? LibUsbResult.NotFound
                : (LibUsbResult)libusb_cancel_transfer(_transferPtr);
        }
    }

    /// <summary>
    /// Throw ObjectDisposedException when LibUsbTransfer is disposed.
    /// </summary>
    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LibUsbTransfer));
        }
    }

    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed)
            {
                return;
            }
            if (_transferPtr != IntPtr.Zero)
            {
                libusb_free_transfer(_transferPtr);
            }
            if (_callbackHandle.IsAllocated)
            {
                _callbackHandle.Free();
            }
            _disposed = true;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void LibUsbTransferCallback(nint transferPtr);

    [StructLayout(LayoutKind.Sequential)]
    internal struct LibUsbTransferTemplate
    {
        /// <summary>
        /// Handle of the device that this transfer will be submitted to.
        /// </summary>
        internal nint DeviceHandle;

        /// <summary>
        /// A bitwise OR combination of libusb_transfer_flags.
        /// </summary>
        internal LibUsbTransferFlag Flags;

        /// <summary>
        /// Address of the endpoint where this transfer will be sent.
        /// </summary>
        internal byte Endpoint;

        /// <summary>
        /// Type of the transfer from libusb_transfer_type.
        /// </summary>
        internal LibUsbTransferType Type;

        /// <summary>
        /// Timeout for this transfer in milliseconds.
        /// </summary>
        internal uint Timeout;

        /// <summary>
        /// The status of the transfer.
        /// </summary>
        internal LibUsbTransferStatus Status;

        /// <summary>
        /// Length of the data buffer.
        /// </summary>
        internal int Length;

        /// <summary>
        /// Actual length of data that was transferred.
        /// </summary>
        internal int ActualLength;

        /// <summary>
        /// Callback function.
        /// </summary>
        internal LibUsbTransferCallback Callback;

        /// <summary>
        /// User context data.
        /// </summary>
        internal nint UserData;

        /// <summary>
        /// Data buffer.
        /// </summary>
        internal nint Buffer;

        /// <summary>
        /// Number of isochronous packets.
        /// </summary>
        internal int NumIsoPackets;

        // Isochronous packet descriptors, for isochronous transfers only.
        // IsoPacketDesc
    }

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
