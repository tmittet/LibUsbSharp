namespace LibUsbSharp;

public static class UsbDeviceExtension
{
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
