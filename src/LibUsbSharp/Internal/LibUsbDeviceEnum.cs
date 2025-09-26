using System.Runtime.InteropServices;
using LibUsbNative;
using LibUsbNative.Enums;
using LibUsbNative.Extensions;
using LibUsbNative.SafeHandles;
using LibUsbNative.Structs;
using LibUsbSharp.Descriptor;
using LibUsbSharp.Internal.Descriptor;
using Microsoft.Extensions.Logging;

namespace LibUsbSharp.Internal;

internal static class LibUsbDeviceEnum
{
    /// <summary>
    /// Get a list of devices. This does not involve any requests being sent to the devices.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="libusbContext">Pointer to the initialized libusb_init context.</param>
    /// <param name="vendorId">Optional vendor ID filter; only return matching devices.</param>
    /// <param name="productIds">Optional product ID filter; only return matching devices.</param>
    internal static List<IUsbDeviceDescriptor> GetDeviceList(
        ILogger logger,
        ISafeContext libusbContext,
        ushort? vendorId,
        HashSet<ushort>? productIds
    )
    {
        // TODO: Verify error handling, behavior has changed with LibUsbNative
        using var deviceList = libusbContext.GetDeviceList();

        return GetDeviceDescriptors(logger, deviceList)
            .Select(d => d.Descriptor)
            .Where(d =>
                (vendorId is null || vendorId == d.VendorId) && (productIds is null || productIds.Contains(d.ProductId))
            )
            .Cast<IUsbDeviceDescriptor>()
            .ToList();
    }

    /// <summary>
    /// Get cached USB device descriptors for a given, alrady in memory, device descriptor list.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="devices">Pointer to device list returned by libusb_get_device_list.</param>
    internal static IEnumerable<(ISafeDevice device, UsbDeviceDescriptor Descriptor)> GetDeviceDescriptors(
        ILogger logger,
        IReadOnlyList<ISafeDevice> devices
    )
    {
        // TODO: Verify error handling, behavior has changed with LibUsbNative

        foreach (var device in devices)
        {
            var result = TryGetDeviceDescriptor(device, out var descriptor);
            if (result != libusb_error.LIBUSB_SUCCESS)
            {
                logger.LogWarning("Get device descriptor failed. {ErrorMessage}.", result.GetString());
            }
            else if (descriptor!.Value.BcdUsb > 0)
            {
                yield return (device, descriptor!.Value);
            }
        }
    }

    /// <summary>
    /// Get the cached USB device descriptor for a given, alrady in memory, device descriptor.
    /// NOTE: since libusb-1.0.16, LIBUSBX_API_VERSION >= 0x01000102, this function always succeeds.
    /// </summary>
    internal static libusb_error TryGetDeviceDescriptor(ISafeDevice device, out UsbDeviceDescriptor? descriptor)
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

    /// <summary>
    /// Get the USB configuration descriptor for the currently active device configuration. This
    /// is a non-blocking function which does not involve any requests being sent to the device.
    /// </summary>
    internal static void GetConfigDescriptor(ISafeDevice device, out IUsbConfigDescriptor? descriptor)
    {
        using var safeConfigDescriptorPtr = device.GetActiveConfigDescriptorPtr();
        descriptor = Marshal
            .PtrToStructure<LibUsbConfigDescriptor>(safeConfigDescriptorPtr.GetUnmanagedPointer())
            .ToUsbInterfaceDescriptor();
    }
}
