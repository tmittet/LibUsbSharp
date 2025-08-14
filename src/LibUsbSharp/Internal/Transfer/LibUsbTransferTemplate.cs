using System.Runtime.InteropServices;
using static LibUsbSharp.Internal.Transfer.LibUsbTransfer;

namespace LibUsbSharp.Internal.Transfer;

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
