using System.Runtime.InteropServices;
using LibUsbNative.Enums;
using LibUsbNative.Structs;

namespace LibUsbNative;

/// <summary>
/// Swappable facade for libusb 1.x. Default impl: <see cref="PInvokeLibUsbApi"/>.
/// </summary>
public interface ILibUsbApi
{
    libusb_error libusb_init(out IntPtr ctx);
    void libusb_exit(IntPtr ctx);
    libusb_error libusb_set_option(IntPtr ctx, libusb_option usbOption, int value);
    libusb_error libusb_set_option(IntPtr ctx, libusb_option usbOption, IntPtr value);

    libusb_error libusb_handle_events_completed(IntPtr ctx, IntPtr completed);

    void libusb_interrupt_event_handler(IntPtr ctx);

    IntPtr libusb_get_version();

    int libusb_has_capability(uint capability);
    IntPtr libusb_strerror(libusb_error errorCode);

    libusb_error libusb_get_device_list(IntPtr ctx, out IntPtr list);
    void libusb_free_device_list(IntPtr list, int unrefDevices);
    void libusb_ref_device(IntPtr dev);
    void libusb_unref_device(IntPtr dev);

    libusb_error libusb_get_device_descriptor(IntPtr dev, out libusb_device_descriptor desc);
    libusb_error libusb_get_active_config_descriptor(IntPtr dev, out IntPtr config);
    libusb_error libusb_get_config_descriptor(IntPtr dev, ushort index, out IntPtr config);
    void libusb_free_config_descriptor(IntPtr config);

    byte libusb_get_bus_number(IntPtr dev);
    byte libusb_get_device_address(IntPtr dev);
    byte libusb_get_port_number(IntPtr dev);

    libusb_error libusb_open(IntPtr dev, out IntPtr handle);
    void libusb_close(IntPtr handle);

    libusb_error libusb_claim_interface(IntPtr handle, int interfaceNumber);

    libusb_error libusb_release_interface(IntPtr handle, int interfaceNumber);
    libusb_error libusb_get_string_descriptor_ascii(IntPtr handle, byte desc_index, byte[] data, int length);

    libusb_error libusb_reset_device(IntPtr handle);

    IntPtr libusb_alloc_transfer(int iso_packets);
    void libusb_free_transfer(IntPtr transfer);
    libusb_error libusb_submit_transfer(IntPtr transfer);
    libusb_error libusb_cancel_transfer(IntPtr transfer);

    libusb_error libusb_hotplug_register_callback(
        IntPtr ctx,
        int events,
        int flags,
        int vendorId,
        int productId,
        int devClass,
        libusb_hotplug_callback_fn cb,
        IntPtr user_data,
        out int callbackHandle
    );
    void libusb_hotplug_deregister_callback(IntPtr ctx, IntPtr callbackHandle);
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int libusb_hotplug_callback_fn(IntPtr ctx, IntPtr dev, int eventType, IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void libusb_log_callback(IntPtr context, int level, string message);
