namespace LibUsbSharp.Internal.ControlTransfer;

static class LibUsbControlTransferExtensions
{
    private static List<string> GetBmControlsForDescriptor(
        int bmStart,
        int bLength,
        byte[] extra,
        int off
    )
    {
        int bmLen = Math.Max(0, bLength - bmStart);
        var bmOutput = new List<string>();
        if (bmLen > 0 && off + bmStart + bmLen <= extra.Length)
        {
            var bm = new byte[bmLen];
            Buffer.BlockCopy(extra, off + bmStart, bm, 0, bmLen);
            Console.Write("    PU bmControls: ");
            for (int k = 0; k < bm.Length; k++)
            {
                var hexStr = $"{bm[k]:X2}";
                bmOutput.Add(hexStr);
            }
        }
        return bmOutput;
    }

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
                            var bmOutput = GetBmControlsForDescriptor(8, bLength, extra, off);
                            // Maybe use this later? But probably fine to hard code capabilities
                            Console.WriteLine(bmOutput);
                        }
                        break;
                    // Same as above
                    case VC_PROCESSING_UNIT:
                        if (bLength >= 5)
                        {
                            byte bUnitID = extra[off + 3];
                            libUsbControlTransfer.ProcessingUnitId = bUnitID;
                            var bmOutput = GetBmControlsForDescriptor(7, bLength, extra, off);
                            // Maybe use this later? But probably fine to hard code capabilities
                            Console.WriteLine(bmOutput);
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
