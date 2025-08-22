using System;
using System.Runtime.InteropServices;

namespace LibUsbSharp.Internal.ControlTransfer;

internal sealed class LibUsbControlTransfer
{
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
                && iface.InterfaceSubClass == VideoSubclass.VideoControl
            )
            {
                VcInterfaceNumber = iface.InterfaceNumber;
                this.ExtractInfoFromExtraBytes(iface.ExtraBytes);
                return;
            }
        }

        throw new InvalidOperationException(
            "Device does not have a video control compatible interface"
        );
    }

    public byte[] ControlTransfer(
        BmRequestType bmRequestType,
        BRequest bRequest,
        Selector selector,
        byte[] buffer,
        int timeoutMs
    )
    {
        ushort wValue = (ushort)((byte)selector << 8);
        ushort wIndex = (ushort)(ProcessingUnitId << 8 | VcInterfaceNumber);
        var bufferSize = (ushort)buffer.Length;
        var response = libusb_control_transfer(
            DeviceHandle,
            (byte)bmRequestType,
            (byte)bRequest,
            wValue,
            wIndex,
            buffer,
            bufferSize,
            timeoutMs
        );

        if (response < 0)
        {
            throw new LibUsbException("Unable to do control transfer", response);
        }

        return buffer;
    }

    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern LibUsbResult libusb_control_transfer(
        nint deviceHandle,
        byte bmRequestType,
        byte bRequest,
        UInt16 wValue,
        UInt16 wIndex,
        byte[] data,
        UInt16 wLength,
        int timeout
    );
}
