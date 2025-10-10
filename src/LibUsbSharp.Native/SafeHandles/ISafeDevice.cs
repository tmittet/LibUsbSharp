using LibUsbSharp.Native.Structs;

namespace LibUsbSharp.Native.SafeHandles;

public interface ISafeDevice
{
    /// <summary>
    /// Open the USB device. Enables you to perform I/O on the device.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    /// <exception cref="LibUsbException">Thrown when the device open operation fails.</exception>
    ISafeDeviceHandle Open();

    /// <summary>
    /// Get the USB device descriptor. NOTE: Since libusb-1.0.16, this function always succeeds.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    libusb_device_descriptor GetDeviceDescriptor();

    /// <summary>
    /// Get the USB configuration descriptor for the currently active configuration.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    /// <exception cref="LibUsbException">Thrown when the get descriptor operation fails.</exception>
    libusb_config_descriptor GetActiveConfigDescriptor();

    /// <summary>
    /// Get a pointer to the USB configuration descriptor for the currently active configuration.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    /// <exception cref="LibUsbException">Thrown when the get pointer operation fails.</exception>
    ISafeConfigDescriptor GetActiveConfigDescriptorPtr();

    /// <summary>
    /// Get a USB configuration descriptor based on its index.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    /// <exception cref="LibUsbException">Thrown when the get descriptor operation fails.</exception>
    libusb_config_descriptor GetConfigDescriptor(byte configIndex);

    /// <summary>
    /// Get a pointer to a USB configuration descriptor based on its index.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    /// <exception cref="LibUsbException">Thrown when the get pointer operation fails.</exception>
    ISafeConfigDescriptor GetConfigDescriptorPtr(byte configIndex);

    /// <summary>
    /// Get the number of the bus that the device is connected to.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    byte GetBusNumber();

    /// <summary>
    /// Get the address of the device on the bus it's connected to.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    byte GetDeviceAddress();

    /// <summary>
    /// Get the number of the port that the device is connected to.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    byte GetPortNumber();
}
