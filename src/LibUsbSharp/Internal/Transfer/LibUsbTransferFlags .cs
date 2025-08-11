namespace LibUsbSharp.Internal.Transfer;

[Flags]
internal enum LibUsbTransferFlag : byte
{
    None = 0,
    ShortNotOk = 1 << 0,
    FreeBuffer = 1 << 1,
    FreeTransfer = 1 << 2,
    AddZeroPacket = 1 << 3,
}
