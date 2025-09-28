using LibUsbSharp.Descriptor;
using LibUsbSharp.Native;
using LibUsbSharp.Transfer;

namespace LibUsbSharp;

public static class UsbDeviceExtension
{
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
    public static IUsbInterface ClaimInterface(this IUsbDevice device, UsbClass withClass, byte? withSubClass = null)
    {
        var usbInterface = device.Interfaces(withClass, withSubClass).FirstOrDefault();
        return usbInterface is null
            ? throw new InvalidOperationException($"Device '{device}' {withClass} interface not found.")
            : device.ClaimInterface(usbInterface);
    }

    /// <summary>
    /// Get interface descriptors matching given parameters.
    /// </summary>
    /// <param name="device">A UsbDevice instance.</param>
    /// <param name="withClass">Interface class filter.</param>
    /// <param name="withSubClass">Optional interface sub-class filter.</param>
    /// <returns>A list of matching interface descriptors.</returns>
    public static IList<IUsbInterfaceDescriptor> GetInterfaceDescriptors(
        this IUsbDevice device,
        UsbClass withClass,
        byte? withSubClass = null
    ) => device.Interfaces(withClass, withSubClass).ToList();

    /// <summary>
    /// Check if device has an interface matching given parameters.
    /// </summary>
    /// <param name="device">A UsbDevice instance.</param>
    /// <param name="withClass">Interface class filter.</param>
    /// <param name="withSubClass">Optional interface sub-class filter.</param>
    /// <returns>True when one or more matching interfaces are found.</returns>
    public static bool HasInterface(this IUsbDevice device, UsbClass withClass, byte? withSubClass = null) =>
        device.Interfaces(withClass, withSubClass).Any();

    private static IEnumerable<IUsbInterfaceDescriptor> Interfaces(
        this IUsbDevice usbDevice,
        UsbClass withClass,
        byte? withSubClass
    ) =>
        usbDevice.ConfigDescriptor.Interfaces.Where(i =>
            i.InterfaceClass == withClass && (withSubClass is null || i.InterfaceSubClass == withSubClass.Value)
        );
}
