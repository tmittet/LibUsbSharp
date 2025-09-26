using System.Runtime.InteropServices;
using LibUsbNative.Enums;
using LibUsbNative.SafeHandles;

namespace LibUsbSharp.Internal.Transfer;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void LibUsbTransferCallback(nint transferPtr);

[StructLayout(LayoutKind.Sequential)]
internal struct LibUsbTransferTemplate
{
    public static LibUsbTransferTemplate Create(
        ISafeDeviceHandle deviceHandle,
        byte endpoint,
        GCHandle bufferHandle,
        int bufferLength,
        libusb_endpoint_transfer_type type,
        uint timeout,
        LibUsbTransferCallback callback
    )
    {
        return new LibUsbTransferTemplate
        {
            DeviceHandle = deviceHandle.DangerousGetHandle(),
            Endpoint = endpoint,
            Type = type,
            Timeout = timeout,
            Length = bufferLength,
            Callback = callback,
            Buffer = bufferHandle.AddrOfPinnedObject(),
        };
    }

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
    internal libusb_endpoint_transfer_type Type;

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

    // TODO: IsoPacketDesc: Isochronous packet descriptors, for isochronous transfers only.

    public override readonly string ToString() =>
        $"Handle=0x{(nuint)DeviceHandle:X}, Ep=0x{Endpoint:X2}, Type={Type}, Flags={Flags}, Status={Status}, Len={Length}, Actual={ActualLength}, Timeout={Timeout}ms, Buf=0x{(nuint)Buffer:X}, User=0x{(nuint)UserData:X}, IsoPkts={NumIsoPackets}";
}
