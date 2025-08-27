using LibUsbSharp.Descriptor;

namespace LibUsbSharp;

public static class UsbDeviceExtension
{
    public static LibUsbResult ControlRead(
        this IUsbDevice device,
        ControlRequestRecipient recipient,
        ControlRequestStandard request,
        ushort value,
        ushort index,
        Span<byte> destination,
        out ushort bytesRead,
        int timeout = Timeout.Infinite
    ) =>
        device.ControlRead(
            recipient,
            ControlRequestType.Standard,
            (byte)request,
            value,
            index,
            destination,
            out bytesRead,
            timeout
        );

    public static LibUsbResult ControlWrite(
        this IUsbDevice device,
        ControlRequestRecipient recipient,
        ControlRequestStandard request,
        ushort value,
        ushort index,
        ReadOnlySpan<byte> source,
        out int bytesWritten,
        int timeout = Timeout.Infinite
    ) =>
        device.ControlWrite(
            recipient,
            ControlRequestType.Standard,
            (byte)request,
            value,
            index,
            source,
            out bytesWritten,
            timeout
        );

    /// <summary>
    /// Claim a USB interface. The interface will be auto-released when the device is disposed.
    /// NOTE: When more than one matching interface is found, the first is selected and claimed.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the USB interface is already claimed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a USB interface of provided class is not found.
    /// </exception>
    /// <exception cref="LibUsbException">
    /// Thrown when the USB interface claim operation fails.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the UsbDevice is disposed.
    /// </exception>
    public static IUsbInterface ClaimInterface(
        this IUsbDevice device,
        UsbClass withClass,
        byte? withSubClass = null
    )
    {
        var usbInterface = device.Interfaces(withClass, withSubClass).FirstOrDefault();
        return usbInterface is null
            ? throw new InvalidOperationException(
                $"Device '{device}' {withClass} interface not found."
            )
            : device.ClaimInterface(usbInterface);
    }

    public static bool HasInterface(
        this IUsbDevice device,
        UsbClass withClass,
        byte? withSubClass = null
    ) => device.Interfaces(withClass, withSubClass).Any();

    private static IEnumerable<IUsbInterfaceDescriptor> Interfaces(
        this IUsbDevice usbDevice,
        UsbClass withClass,
        byte? withSubClass
    ) =>
        usbDevice.ConfigDescriptor.Interfaces.Where(i =>
            i.InterfaceClass == withClass
            && (withSubClass is null || i.InterfaceSubClass == withSubClass.Value)
        );
}
