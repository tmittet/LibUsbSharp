using Microsoft.Extensions.Logging;
using UsbDotNet.Descriptor;
using UsbDotNet.LibUsbNative;
using UsbDotNet.LibUsbNative.Enums;
using UsbDotNet.LibUsbNative.Extensions;
using UsbDotNet.LibUsbNative.SafeHandles;
using UsbDotNet.LibUsbNative.Structs;

namespace UsbDotNet.Internal;

internal static class UsbDeviceEnum
{
    /// <summary>
    /// Gets a list of USB devices. This does not involve any requests being sent to the devices.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="libusbContext">Pointer to the initialized libusb_init context.</param>
    /// <param name="vendorId">Optional vendor ID filter; only return matching devices.</param>
    /// <param name="productIds">Optional product ID filter; only return matching devices.</param>
    /// <exception cref="ObjectDisposedException">Thrown when context is disposed.</exception>
    /// <exception cref="LibUsbException">Thrown when the get device list operation fails.</exception>
    internal static List<IUsbDeviceDescriptor> GetDeviceList(
        ILogger logger,
        ISafeContext libusbContext,
        ushort? vendorId,
        HashSet<ushort>? productIds
    )
    {
        using var deviceList = libusbContext.GetDeviceList();

        return GetDeviceDescriptors(logger, deviceList)
            .Select(d => d.Descriptor)
            .Where(d =>
                (vendorId is null || vendorId == d.VendorId)
                && (productIds is null || productIds.Contains(d.ProductId))
            )
            .Cast<IUsbDeviceDescriptor>()
            .ToList();
    }

    /// <summary>
    /// Get cached USB device descriptors for a given, already in memory, device descriptor list.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="devices">Pointer to device list returned by libusb_get_device_list.</param>
    /// <exception cref="ObjectDisposedException">Thrown when device is disposed.</exception>
    internal static IEnumerable<(
        ISafeDevice device,
        UsbDeviceDescriptor Descriptor
    )> GetDeviceDescriptors(ILogger logger, IReadOnlyList<ISafeDevice> devices)
    {
        foreach (var device in devices)
        {
            var result = TryGetDeviceDescriptor(device, out var descriptor);
            // NOTE: Should always be LIBUSB_SUCCESS; since libusb-1.0.16 libusb_get_device_descriptor always succeeds.
            if (result != libusb_error.LIBUSB_SUCCESS)
            {
                logger.LogWarning(
                    "Get device descriptor failed: {ErrorMessage}.",
                    result.GetString()
                );
            }
            else if (descriptor!.Value.BcdUsb > 0)
            {
                yield return (device, descriptor!.Value);
            }
        }
    }

    /// <summary>
    /// Get the cached USB device descriptor for a given, already in memory, device descriptor.
    /// NOTE: since libusb-1.0.16, LIBUSBX_API_VERSION >= 0x01000102, this function always succeeds.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when device is disposed.</exception>
    internal static libusb_error TryGetDeviceDescriptor(
        ISafeDevice device,
        out UsbDeviceDescriptor? descriptor
    )
    {
        libusb_device_descriptor partialDescriptor;
        try
        {
            partialDescriptor = device.GetDeviceDescriptor();
        }
        catch (LibUsbException ex)
        {
            descriptor = null;
            return ex.Error;
        }
        descriptor = new UsbDeviceDescriptor(
            partialDescriptor,
            device.GetBusNumber(),
            device.GetDeviceAddress(),
            device.GetPortNumber()
        );
        return libusb_error.LIBUSB_SUCCESS;
    }
}
