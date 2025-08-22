namespace LibUsbSharp;

public static class UsbDeviceExtension
{
    /// <summary>
    /// Claim a USB interface. The interface will be auto-released when the device is disposed.
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
    public static IUsbInterface ClaimInterface(this IUsbDevice device, UsbClass interfaceClass)
    {
        var usbInterface = device.ConfigDescriptor.Interfaces.FirstOrDefault(i =>
            i.InterfaceClass == interfaceClass
        );
        return usbInterface is null
            ? throw new InvalidOperationException(
                $"Device '{device}' {interfaceClass} interface not found."
            )
            : device.ClaimInterface(usbInterface);
    }

    public static bool HasInterface(this IUsbDevice usbDevice, UsbClass interfaceClass)
    {
        return usbDevice.ConfigDescriptor.Interfaces.Any(i => i.InterfaceClass == interfaceClass);
    }
    
}
