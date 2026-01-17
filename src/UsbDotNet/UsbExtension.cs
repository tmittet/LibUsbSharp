using UsbDotNet.Descriptor;
using UsbDotNet.LibUsbNative;

namespace UsbDotNet;

public static class UsbExtension
{
    /// <summary>
    /// Returns a list of device descriptors for connected USB devices.
    /// It does not involve any requests being sent to the devices.
    /// </summary>
    /// <param name="libUsb" />
    /// <param name="vendorId">Optional vendor ID filter.</param>
    /// <param name="productId">Optional product ID filter.</param>
    /// <exception cref="LibUsbException">Thrown when the get device list operation fails.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the Usb type is disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Usb type is not initialized.</exception>
    public static List<IUsbDeviceDescriptor> GetDeviceList(
        this IUsb libUsb,
        ushort? vendorId = default,
        params ushort[] productId
    ) => libUsb.GetDeviceList(vendorId, productId.ToHashSet());

    /// <summary>
    /// Get the device serial number. To read the serial the device must be opened for a brief
    /// moment; unless already open. If the device is open in another process the read will fail.
    /// </summary>
    /// <exception cref="LibUsbException">
    /// LibUsbException ErrorCode AccessDenied or IO is typically an indication that the device
    /// is inaccessible; because it's open in another process or because of lacking permissions.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Usb type is not initialized.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the Usb type is disposed.</exception>
    public static string GetDeviceSerial(this IUsb libUsb, IUsbDeviceDescriptor descriptor) =>
        libUsb.GetDeviceSerial(descriptor.DeviceKey);

    /// <summary>
    /// Opens the USB device without claiming any device interfaces or reading device serial.
    /// This is a non-blocking function; no requests are sent over the USB bus.
    /// </summary>
    /// <exception cref="LibUsbException">
    /// LibUsbException ErrorCode AccessDenied or IO is typically an indication that the device
    /// is inaccessible; because it's open in another process or because of lacking permissions.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Usb type is not initialized.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the Usb type is disposed.</exception>
    public static IUsbDevice OpenDevice(this IUsb libUsb, IUsbDeviceDescriptor descriptor) =>
        libUsb.OpenDevice(descriptor.DeviceKey);
}
