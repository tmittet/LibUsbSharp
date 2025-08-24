using System.Runtime.InteropServices;
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
        nint libusbContext,
        ushort? vendorId,
        HashSet<ushort>? productIds
    )
    {
        var result = libusb_get_device_list(libusbContext, out var listPtr);
        try
        {
            return result >= 0
                ? GetDeviceDescriptors(logger, listPtr)
                    .Select(d => d.Descriptor)
                    .Where(d =>
                        (vendorId is null || vendorId == d.VendorId)
                        && (productIds is null || productIds.Contains(d.ProductId))
                    )
                    .Cast<IUsbDeviceDescriptor>()
                    .ToList()
                : throw LibUsbException.FromError(result, "Failed to get device list.");
        }
        finally
        {
            libusb_free_device_list(listPtr, 1);
        }
    }

    /// <summary>
    /// Get cached USB device descriptors for a given, alrady in memory, device descriptor list.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="listPtr">Pointer to device list returned by libusb_get_device_list.</param>
    internal static IEnumerable<(
        nint DescriptorPtr,
        UsbDeviceDescriptor Descriptor
    )> GetDeviceDescriptors(ILogger logger, nint listPtr)
    {
        var offset = 0;
        nint descriptorPtr;
        while ((descriptorPtr = Marshal.ReadIntPtr(listPtr, offset)) != IntPtr.Zero)
        {
            var result = TryGetDeviceDescriptor(descriptorPtr, out var descriptor);
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
                yield return (descriptorPtr, descriptor!.Value);
            }
            offset += IntPtr.Size;
        }
    }

    /// <summary>
    /// Get the cached USB device descriptor for a given, alrady in memory, device descriptor.
    /// NOTE: since libusb-1.0.16, LIBUSBX_API_VERSION >= 0x01000102, this function always succeeds.
    /// </summary>
    internal static LibUsbResult TryGetDeviceDescriptor(
        nint deviceDescriptorPtr,
        out UsbDeviceDescriptor? descriptor
    )
    {
        descriptor = null;
        var result = libusb_get_device_descriptor(deviceDescriptorPtr, out var partialDescriptor);
        if (result == 0)
        {
            descriptor = new UsbDeviceDescriptor(
                partialDescriptor,
                libusb_get_bus_number(deviceDescriptorPtr),
                libusb_get_device_address(deviceDescriptorPtr),
                libusb_get_port_number(deviceDescriptorPtr)
            );
        }
        return (LibUsbResult)result;
    }

    /// <summary>
    /// Get the USB configuration descriptor for the currently active device configuration. This
    /// is a non-blocking function which does not involve any requests being sent to the device.
    /// </summary>
    internal static LibUsbResult TryGetConfigDescriptor(
        nint deviceDescriptorPtr,
        out IUsbConfigDescriptor? descriptor
    )
    {
        descriptor = null;
        var result = libusb_get_active_config_descriptor(deviceDescriptorPtr, out var configPtr);
        if (result != 0)
        {
            return (LibUsbResult)result;
        }
        try
        {
            descriptor = Marshal
                .PtrToStructure<LibUsbConfigDescriptor>(configPtr)
                .ToUsbInterfaceDescriptor();
        }
        finally
        {
            libusb_free_config_descriptor(configPtr);
        }
        return LibUsbResult.Success;
    }

    // LibraryImportAttribute not available in .NET6, silence warning
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'

    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_get_device_list(IntPtr context, out IntPtr listPtr);

    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_free_device_list(IntPtr listPtr, int unrefDevices);

    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_get_device_descriptor(
        IntPtr deviceDescriptorPtr,
        out LibUsbDeviceDescriptor deviceDescriptor
    );

    /// <summary>
    /// Get the number of the bus that a device is connected to.
    /// </summary>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern byte libusb_get_bus_number(IntPtr deviceDescriptorPtr);

    /// <summary>
    /// Get the address of the device on the bus it is connected to.
    /// </summary>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern byte libusb_get_device_address(IntPtr deviceDescriptorPtr);

    /// <summary>
    /// Get the number of the port that a device is connected to.
    ///
    /// The number returned by this call is usually guaranteed to be uniquely tied to a physical
    /// port, meaning that different devices plugged on the same physical port should return the
    /// same port number. But there is no guarantee that the port number returned by this call will
    /// remain the same, or even match the order in which ports are numbered on the HUB/HCD.
    /// </summary>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern byte libusb_get_port_number(IntPtr deviceDescriptorPtr);

    /// <summary>
    /// Get the USB configuration descriptor for the currently active configuration. This is
    /// a non-blocking function which does not involve any requests being sent to the device.
    /// </summary>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_get_active_config_descriptor(
        IntPtr deviceDescriptorPtr,
        out IntPtr deviceConfigPtr
    );

    /// <summary>
    /// Free a configuration descriptor obtained from
    /// libusb_get_active_config_descriptor() or libusb_get_config_descriptor()
    /// </summary>
    /// <param name="configPtr"></param>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_free_config_descriptor(IntPtr configPtr);

#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'
}
