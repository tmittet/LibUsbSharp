using System.Runtime.InteropServices;
using LibUsbNative.SafeHandles;
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
        (var deviceList, var count) = libusbContext.GetDeviceList();

        var result = LibUsbResult.Success;
        try
        {
            return result >= 0
                ? GetDeviceDescriptors(logger, deviceList.Devices.ToList())
                    .Select(d => d.Descriptor)
                    .Where(d =>
                        (vendorId is null || vendorId == d.VendorId)
                        && (productIds is null || productIds.Contains(d.ProductId))
                    )
                    .Cast<IUsbDeviceDescriptor>()
                    .ToList()
                : throw LibUsbException.FromResult(result, "Failed to get device list.");
        }
        finally
        {
            deviceList.Dispose();
        }
    }

    /// <summary>
    /// Get cached USB device descriptors for a given, alrady in memory, device descriptor list.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="devices">Pointer to device list returned by libusb_get_device_list.</param>
    internal static IEnumerable<(ISafeDevice device, UsbDeviceDescriptor Descriptor)> GetDeviceDescriptors(
        ILogger logger,
        List<ISafeDevice> devices
    )
    {
        foreach (var device in devices)
        {
            var result = TryGetDeviceDescriptor(device, out var descriptor);
            if (result != LibUsbResult.Success)
            {
                logger.LogWarning(
                    "{LibUsb} get device descriptor failed. {ErrorMessage}",
                    LibUsb.LibraryName,
                    result.GetMessage()
                );
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
    internal static LibUsbResult TryGetDeviceDescriptor(ISafeDevice device, out UsbDeviceDescriptor? descriptor)
    {
        //var partialDescriptor = device.GetActiveConfigDescriptorPtr().DangerousGetHandle();
        var partialDescriptor = device.GetDeviceDescriptor();

        descriptor = new UsbDeviceDescriptor(
            partialDescriptor,
            device.GetBusNumber(),
            device.GetDeviceAddress(),
            device.GetPortNumber()
        );
        return LibUsbResult.Success;
    }

    /// <summary>
    /// Get the USB configuration descriptor for the currently active device configuration. This
    /// is a non-blocking function which does not involve any requests being sent to the device.
    /// </summary>
    internal static LibUsbResult TryGetConfigDescriptor(ISafeDevice device, out IUsbConfigDescriptor? descriptor)
    {
        descriptor = null;
        try
        {
            using var safeConfigDescriptorPtr = device.GetActiveConfigDescriptorPtr();
            descriptor = Marshal
                .PtrToStructure<LibUsbConfigDescriptor>(safeConfigDescriptorPtr.DangerousGetHandle())
                .ToUsbInterfaceDescriptor();
            return LibUsbResult.Success;
        }
        catch (LibUsbNative.LibUsbException ex)
        {
            return (LibUsbResult)ex.Error;
        }
    }
}
