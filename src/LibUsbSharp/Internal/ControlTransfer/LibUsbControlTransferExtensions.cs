using System;

namespace LibUsbSharp.Internal.ControlTransfer;

public struct ExtraDescriptorIds
{
    byte CameraTerminalId;
    byte ProcessingUnitId;
}

static class LibUsbControlTransferExtensions
{
    internal static void ExtractInfoFromExtraBytes(
        this LibUsbControlTransfer libUsbControlTransfer,
        byte[] extra
    )
    {
        const byte CLASS_SPECIFIC = 0x24;
        const byte VC_INPUT_TERMINAL = 0x02;
        const byte VC_PROCESSING_UNIT = 0x05;
        const byte VC_EXTENSION_UNIT = 0x06;
        const ushort ITT_CAMERA = 0x0201;

        int off = 0;
        // Descriptor must be at least 4 in length
        while (off + 3 <= extra.Length)
        {
            byte bLength = extra[off];
            if (bLength == 0 || off + bLength > extra.Length)
                break;

            byte bDescriptorType = extra[off + 1];
            byte bDescriptorSubType = extra[off + 2];

            if (bDescriptorType == CLASS_SPECIFIC)
            {
                switch (bDescriptorSubType)
                {
                    case VC_INPUT_TERMINAL:
                        // Ensuring that we have the whole descriptor as 8 is the shortes possible variant.
                        if (bLength >= 8)
                        {
                            byte bTerminalID = extra[off + 3];
                            ushort wTerminalType = BitConverter.ToUInt16(extra, off + 4);
                            if (wTerminalType == ITT_CAMERA && bTerminalID != 0)
                                libUsbControlTransfer.CameraTerminalId = bTerminalID;
                        }
                        break;
                    // Same as above
                    case VC_PROCESSING_UNIT:
                        if (bLength >= 5)
                        {
                            byte bUnitID = extra[off + 3];
                            libUsbControlTransfer.ProcessingUnitId = bUnitID;
                        }
                        break;

                    case VC_EXTENSION_UNIT:
                        // TODO: Do something about XU support here maybe
                        break;
                }
            }

            off += bLength;
        }
    }
}
