using System;
using System.Runtime.InteropServices;

namespace LibUsbSharp.Internal.ControlTransfer;

internal sealed class LibUsbControlTransfer
{
    private const byte UVC_GET_CUR = 0x81;
    private const byte UVC_GET_MIN = 0x82;
    private const byte UVC_GET_MAX = 0x83;
    private const byte UVC_GET_RES = 0x84;
    private const byte UVC_GET_LEN = 0x85;
    private const byte UVC_GET_INFO = 0x86;
    const byte PU_BRIGHTNESS_CONTROL = 0x02;
    private const byte CT_ZOOM_ABSOLUTE_CONTROL = 0x0B;

    private const byte BMRT_GET_INTERFACE = 0xA1;
    private const byte UVC_INPUT_TERMINAL_ID = 0x03;
    private const byte SC_VIDEOCONTROL = 0x01;

    public byte VcInterfaceNumber { get; set; }
    public byte CameraTerminalId { get; set; }
    public byte ProcessingUnitId { get; set; }
    nint DeviceHandle { get; init; }

    internal LibUsbControlTransfer(UsbDevice usbDevice)
    {
        DeviceHandle = usbDevice.Handle;
        GetInterfaceEntities(usbDevice);
    }

    private void GetInterfaceEntities(UsbDevice usbDevice)
    {
        foreach (var iface in usbDevice.ConfigDescriptor.Interfaces)
        {
            if (
                iface.InterfaceClass == UsbClass.Video
                && iface.InterfaceSubClass == SC_VIDEOCONTROL
            )
            {
                VcInterfaceNumber = iface.InterfaceNumber;
                this.ExtractInfoFromExtraBytes(iface.ExtraBytes);
            }
        }

        throw new InvalidOperationException(
            "Device does not have a video control compatible interface"
        );
    }

    /*     byte vcInterface = 0x00;
            byte cameraTerminalId = 0x02;
            byte processingUnitId = 0x03; */

    static (int rc, byte info) GetInfo(
        IntPtr dev,
        ushort wValue,
        ushort wIndex,
        uint timeoutMs = 1000
    )
    {
        var buf = new byte[1];
        int r = libusb_control_transfer(
            dev,
            BMRT_GET_INTERFACE,
            UVC_GET_INFO,
            wValue,
            wIndex,
            buf,
            1,
            timeoutMs
        );
        return (r < 0) ? (r, (byte)0) : (r, buf[0]);
    }

    static (int rc, ushort len) GetLen(
        IntPtr dev,
        ushort wValue,
        ushort wIndex,
        uint timeoutMs = 1000
    )
    {
        var buf = new byte[2];
        int r = libusb_control_transfer(
            dev,
            BMRT_GET_INTERFACE,
            UVC_GET_LEN,
            wValue,
            wIndex,
            buf,
            2,
            timeoutMs
        );
        if (r < 0)
            return (r, 0);
        return (r, (ushort)(buf[0] | (buf[1] << 8)));
    }

    // Generic getter that takes the bRequest explicitly
    static (int rc, byte[] data) GetCtrl(
        IntPtr dev,
        byte request,
        ushort wValue,
        ushort wIndex,
        ushort length,
        uint timeoutMs = 1000
    )
    {
        var buf = new byte[length];
        int r = libusb_control_transfer(
            dev,
            BMRT_GET_INTERFACE,
            request,
            wValue,
            wIndex,
            buf,
            length,
            timeoutMs
        );
        return (r < 0) ? (r, Array.Empty<byte>()) : (r, buf);
    }

    // Optional: small helpers for LE16
    static short ToI16LE(byte[] d) => (short)(d[0] | (d[1] << 8));

    static ushort ToU16LE(byte[] d) => (ushort)(d[0] | (d[1] << 8));

    static byte[] FromI16LE(short v) => new[] { (byte)(v & 0xFF), (byte)((v >> 8) & 0xFF) };

    static void SetVal(IntPtr dev, ushort wValue, ushort wIndex, uint timeoutMs = 1000)
    {
        ushort len = 2;
        var (rLen, lenBuf) = GetCtrl(dev, UVC_GET_LEN, wValue, wIndex, 2, timeoutMs);
        if (rLen >= 0 && lenBuf.Length == 2)
        {
            var ctrlLen = ToU16LE(lenBuf);
            if (ctrlLen != 0 && ctrlLen <= 64)
                len = ctrlLen;
        }

        // Current
        var (rCur, curData) = GetCtrl(dev, UVC_GET_CUR, wValue, wIndex, len, timeoutMs);
        if (rCur < 0 || curData.Length < 2)
        {
            Console.WriteLine($"GET_CUR failed: {rCur}");
            return;
        }
        short curVal = ToI16LE(curData);
        Console.WriteLine($"Current value = {curVal}");

        // Range
        var (rMin, minData) = GetCtrl(dev, UVC_GET_MIN, wValue, wIndex, len, timeoutMs);
        var (rMax, maxData) = GetCtrl(dev, UVC_GET_MAX, wValue, wIndex, len, timeoutMs);

        if (rMin < 0 || rMax < 0 || minData.Length < 2 || maxData.Length < 2)
        {
            Console.WriteLine(
                $"GET_MIN/GET_MAX failed: rMin={rMin} rMax={rMax}. Using simple +/-10 clamp around current."
            );
            // Fallback: if range unavailable, just try to nudge
            short tryVal = (short)(curVal + 10);
            byte[] buf = FromI16LE(tryVal);
            int rc = libusb_control_transfer(
                dev,
                BmRequestType.SetInterface,
                BRequest.SetCurrent,
                wValue,
                wIndex,
                buf,
                (ushort)buf.Length,
                timeoutMs
            );
            Console.WriteLine(rc >= 0 ? $"Set to {tryVal}" : $"SET_CUR failed: {rc}");
            return;
        }

        short minVal = ToI16LE(minData);
        short maxVal = ToI16LE(maxData);
        Console.WriteLine($"Range: min={minVal} max={maxVal}");

        // 3) Choose a new value within range (example: +10)
        short newVal = (short)Math.Clamp(-400.0, minVal, maxVal);

        // 4) Write it
        byte[] outBuf = FromI16LE(newVal);
        int rcSet = libusb_control_transfer(
            dev,
            BmRequestType.SetInterface,
            BRequest.SetCurrent,
            wValue,
            wIndex,
            outBuf,
            (ushort)outBuf.Length,
            timeoutMs
        );
        Console.WriteLine(rcSet >= 0 ? $"Set to {newVal}" : $"SET_CUR failed: {rcSet}");
    }

    static (int rc, byte[] data) GetCur(
        IntPtr dev,
        ushort wValue,
        ushort wIndex,
        ushort length,
        uint timeoutMs = 1000
    )
    {
        var buf = new byte[length];
        int r = libusb_control_transfer(
            dev,
            BMRT_GET_INTERFACE,
            UVC_GET_CUR,
            wValue,
            wIndex,
            buf,
            length,
            timeoutMs
        );
        return (r < 0) ? (r, Array.Empty<byte>()) : (r, buf);
    }

    internal void ControlTransfer()
    {
        ushort wValue = (ushort)(PU_BRIGHTNESS_CONTROL << 8);
        ushort wIndex = (ushort)(UVC_INPUT_TERMINAL_ID << 8 | InterfaceEntities.VcInterfaceNumber);
        //var buffer = GCHandle.Alloc(16, GCHandleType.Pinned);

        // Example: Read Brightness with fallbacks
        (int rInfoB, byte infoB) = GetInfo(DeviceHandle, wValue, wIndex);
        var infoString =
            $"GET_INFO Brightness -> rc={rInfoB}, info={((rInfoB >= 0) ? $"0x{infoB:X2}" : "-")}";
        Console.WriteLine(
            $"GET_INFO Brightness -> rc={rInfoB}, info={((rInfoB >= 0) ? $"0x{infoB:X2}" : "-")}"
        );
        // Bit0=GET, Bit1=SET
        if (rInfoB >= 0 && (infoB & 0x01) != 0)
        {
            (int rLenB, ushort lenB) = GetLen(DeviceHandle, wValue, wIndex);
            if (rLenB < 0 || lenB == 0)
                lenB = 2; // common fallback
            var (rCurB, dataB) = GetCur(DeviceHandle, wValue, wIndex, lenB);
            if (rCurB >= 0)
            {
                // interpret as little-endian signed if lenB==2 (common)
                int val = 0;
                for (int i = 0; i < dataB.Length; i++)
                    val |= (dataB[i] & 0xFF) << (8 * i);
                Console.WriteLine($"Brightness raw = {val} (len={dataB.Length})");
            }
            else
            {
                Console.WriteLine($"GET_CUR Brightness failed: {rCurB}");
            }
        }
        else
        {
            Console.WriteLine("Brightness GET not supported or GET_INFO failed.");
        }
        SetVal(DeviceHandle, wValue, wIndex);
        /*  byte[] data = new byte[2];
           int res = libusb_control_transfer(
               device.Handle,
               BMRT_GET_INTERFACE,
               UVC_GET_INFO,
               wValue,
               wIndex,
               data,
               2,
               Timeout.Infinite
           ); */

        /* ushort ctrlLen = (ushort)(lenBuf[0] | (lenBuf[1] << 8));

        byte[] data = new byte[ctrlLen];
        int r = libusb_control_transfer(
            deviceHandle,
            BMRT_GET_INTERFACE,
            UVC_GET_CUR,
            wValue,
            wIndex,
            data,
            ctrlLen,
            1000
        ); */
    }

    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_control_transfer(
        nint deviceHandle,
        byte bmRequestType,
        byte bRequest,
        UInt16 wValue,
        UInt16 wIndex,
        byte[] data,
        UInt16 wLength,
        uint timeout
    );
}
