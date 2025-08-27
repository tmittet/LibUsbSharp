using System.Runtime.InteropServices;
using LibUsbSharp.Transfer;

namespace LibUsbSharp.Internal.Transfer;

/// <summary>
/// A LibUsbControlRequestSetup struct for Control Transfer input/read and output/wrote requests.
/// The struct forms the 8 byte header/setup packet. It may or may not be followed by a payload.
/// </summary>
/// <param name="RequestType">
/// Bit 7 (Direction)     | 0 = Host -> Device (OUT) 1 = Device -> Host (IN).<br />
/// Bits 6..5 (Type)      | 00 = Standard, 01 = Class, 10 = Vendor, 11 = Reserved.<br />
/// Bits 4..0 (Recipient) | 00000 = Device, 00001 = Interface, 00010 = Endpoint, 00011 = Other,
/// 00100-11111 = Reserved.
/// </param>
/// <param name="Request">The USB standard spec, class spec or vendor defined request</param>
/// <param name="Value">The value field for the setup packet</param>
/// <param name="Index">The index field for the setup packet</param>
/// <param name="Length">Read/write payload length or 0 when request has no payload.</param>
// Use pack 1; the fields should be layed out in memory without padding
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal record struct LibUsbControlRequestSetup(
    byte RequestType,
    byte Request,
    ushort Value,
    ushort Index,
    ushort Length
)
{
    internal const int Size = 8; // Setup packet is exactly 8 bytes without padding

    /// <summary>
    /// Create an 8 byte read request LibUsbControlRequestSetup struct from given parameters.
    /// </summary>
    internal static LibUsbControlRequestSetup Read(
        ControlRequestRecipient recipient,
        ControlRequestRequest request,
        ushort length
    ) => Any(ControlRequestDirection.In, recipient, request, length);

    /// <summary>
    /// Create an 8 byte write request LibUsbControlRequestSetup struct from given parameters.
    /// </summary>
    internal static LibUsbControlRequestSetup Write(
        ControlRequestRecipient recipient,
        ControlRequestRequest request,
        ushort length
    ) => Any(ControlRequestDirection.Out, recipient, request, length);

    /// Create an 8 byte LibUsbControlRequestSetup struct from given parameters.
    private static LibUsbControlRequestSetup Any(
        ControlRequestDirection direction,
        ControlRequestRecipient recipient,
        ControlRequestRequest request,
        ushort length
    ) =>
        new(
            RequestType: (byte)((byte)direction << 7 | (byte)request.RawType << 5 | (byte)recipient),
            Request: request.RawRequest,
            Value: request.RawValue,
            Index: request.RawIndex,
            Length: length
        );

    /// <summary>
    /// Create a byte array that consists of the setup bytes + space for given Length of data.
    /// </summary>
    internal byte[] CreateBuffer()
    {
        var buffer = new byte[Size + Length];
        // This writes with host endianness. libusb expects little-endian fields.
        // On mainstream platforms (.NET on x86/x64/ARM LE) this should be fine.
#pragma warning disable CS9191 // .NET6, silence warning. The 'ref' modifier for an argument corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
        MemoryMarshal.Write(buffer, ref this);
#pragma warning restore CS9191 // .NET6, silence warning. The 'ref' modifier for an argument corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
        return buffer;
    }
}
