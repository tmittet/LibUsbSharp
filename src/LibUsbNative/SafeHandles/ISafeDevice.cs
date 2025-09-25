using LibUsbNative.Structs;

namespace LibUsbNative.SafeHandles;

public interface ISafeDevice
{
    /// <summary>
    /// Open the USB device.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    /// <exception cref="LibUsbException">Thrown when the device open operation fails.</exception>
    ISafeDeviceHandle Open();

    /// <summary>
    /// Get the device descriptor. NOTE: Since libusb-1.0.16, this function always succeeds.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    libusb_device_descriptor GetDeviceDescriptor();

    /// <summary>
    /// Get a pointer to the active config descriptor.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    /// <exception cref="LibUsbException">Thrown when the get descriptor operation fails.</exception>
    libusb_config_descriptor GetActiveConfigDescriptor();

    /// <summary>
    /// Get a pointer to the active config descriptor.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    /// <exception cref="LibUsbException">Thrown when the get pointer operation fails.</exception>
    ISafeConfigDescriptorPtr GetActiveConfigDescriptorPtr();

    /// <summary>
    /// Get the config descriptor.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    /// <exception cref="LibUsbException">Thrown when the get descriptor operation fails.</exception>
    libusb_config_descriptor GetConfigDescriptor(byte configIndex);

    /// <summary>
    /// Get a pointer to the config descriptor.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDevice is disposed.</exception>
    /// <exception cref="LibUsbException">Thrown when the get pointer operation fails.</exception>
    ISafeConfigDescriptorPtr GetConfigDescriptorPtr(byte configIndex);

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
