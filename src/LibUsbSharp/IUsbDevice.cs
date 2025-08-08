using LibUsbSharp.Descriptor;

namespace LibUsbSharp;

public interface IUsbDevice : IDisposable
{
    /// <summary>
    /// A device descriptor that includes device class, vendor ID, product ID, bus address and more.
    /// It is safe to read/inspect 'Descriptor' information after the UsbDevice has been disposed.
    /// </summary>
    IUsbDeviceDescriptor Descriptor { get; }

    /// <summary>
    /// A device config descriptor that includes information about device interfaces and endpoints.
    /// It is safe to read/inspect 'ConfigDescriptor' info after the UsbDevice has been disposed.
    /// </summary>
    IUsbConfigDescriptor ConfigDescriptor { get; init; }

    /// <summary>
    /// Reads the manufacturer from the device if required; otherwise a cached value is returned.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the UsbDevice is disposed.</exception>
    string GetManufacturer();

    /// <summary>
    /// Reads the product name from the device if required; otherwise a cached value is returned.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the UsbDevice is disposed.</exception>
    string GetProductName();

    /// <summary>
    /// Reads the serial number from the device if required; otherwise a cached value is returned.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the UsbDevice is disposed.</exception>
    string GetSerialNumber();

    /// <summary>
    /// Reads a string descriptor from the device, using the first language supported by the device.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the UsbDevice is disposed.</exception>
    string ReadStringDescriptor(byte descriptorIndex);

    /// <summary>
    /// Claim a USB interface. The interface will be auto-released when the device is disposed.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the USB interface is already claimed.
    /// </exception>
    /// <exception cref="LibUsbException">
    /// Thrown when the USB interface claim operation fails.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the UsbDevice is disposed.
    /// </exception>
    IUsbInterface ClaimInterface(IUsbInterfaceDescriptor descriptor);

    /// <summary>
    /// Optionally release a USB interface. If not released by calling this method,
    /// the interface will be automatically released when the device is disposed.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the USB interface is not claimed.
    /// </exception>
    /// <exception cref="LibUsbException">
    /// Thrown when the USB interface release operation fails.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the UsbDevice is disposed.
    /// </exception>
    void ReleaseInterface(byte interfaceNumber);

    /// <summary>
    /// WARNING: Use very carefully! Performs a USB port reset to reconnect/reinitialize the device.
    /// The system will attempt to restore the previous configuration and alternate settings after
    /// the reset has completed. If the reset fails, the descriptors change, or the previous state
    /// cannot be restored, the device will appear to be disconnected and reconnected.
    /// </summary>
    void Reset();
}
