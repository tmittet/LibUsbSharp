using System.Runtime.InteropServices;
using LibUsbNative.Descriptors;
using LibUsbNative.Enums;

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

    void libusb_set_debug(IntPtr ctx, int level);
    IntPtr libusb_get_version();

    int libusb_has_capability(uint capability);
    IntPtr libusb_strerror(libusb_error errorCode);

    libusb_error libusb_get_device_list(IntPtr ctx, out IntPtr list);
    void libusb_free_device_list(IntPtr list, int unrefDevices);
    void libusb_ref_device(IntPtr dev);
    void libusb_unref_device(IntPtr dev);

    libusb_error libusb_get_device_descriptor(IntPtr dev, out native_libusb_device_descriptor desc);
    libusb_error libusb_get_active_config_descriptor(IntPtr dev, out IntPtr config);
    libusb_error libusb_get_config_descriptor(IntPtr dev, ushort index, out IntPtr config);
    void libusb_free_config_descriptor(IntPtr config);

    byte libusb_get_bus_number(IntPtr dev);
    byte libusb_get_device_address(IntPtr dev);
    byte libusb_get_port_number(IntPtr dev);
    int libusb_get_port_numbers(IntPtr dev, byte[] port_numbers, int len);
    IntPtr libusb_get_parent(IntPtr dev);
    int libusb_get_device_speed(IntPtr dev);
    int libusb_get_max_packet_size(IntPtr dev, byte endpoint);
    int libusb_get_max_iso_packet_size(IntPtr dev, byte endpoint);

    libusb_error libusb_open(IntPtr dev, out IntPtr handle);
    void libusb_close(IntPtr handle);

    libusb_error libusb_set_configuration(IntPtr handle, int configuration);
    libusb_error libusb_get_configuration(IntPtr handle, out int configuration);
    libusb_error libusb_claim_interface(IntPtr handle, int interfaceNumber);
    libusb_error libusb_release_interface(IntPtr handle, int interfaceNumber);
    libusb_error libusb_set_interface_alt_setting(IntPtr handle, int interfaceNumber, int alternateSetting);

    libusb_error libusb_kernel_driver_active(IntPtr handle, int interfaceNumber);
    libusb_error libusb_detach_kernel_driver(IntPtr handle, int interfaceNumber);
    libusb_error libusb_attach_kernel_driver(IntPtr handle, int interfaceNumber);
    libusb_error libusb_set_auto_detach_kernel_driver(IntPtr handle, int enable);

    libusb_error libusb_get_string_descriptor_ascii(IntPtr handle, byte desc_index, byte[] data, int length);
    libusb_error libusb_get_string_descriptor(IntPtr handle, byte desc_index, ushort langid, byte[] data, int length);

    libusb_error libusb_control_transfer(
        IntPtr handle,
        byte bmRequestType,
        byte bRequest,
        ushort wValue,
        ushort wIndex,
        byte[] data,
        ushort wLength,
        uint timeout
    );
    libusb_error libusb_bulk_transfer(
        IntPtr handle,
        byte endpoint,
        byte[] data,
        int length,
        out int transferred,
        uint timeout
    );
    libusb_error libusb_interrupt_transfer(
        IntPtr handle,
        byte endpoint,
        byte[] data,
        int length,
        out int transferred,
        uint timeout
    );

    libusb_error libusb_clear_halt(IntPtr handle, byte endpoint);
    libusb_error libusb_reset_device(IntPtr handle);

    libusb_error libusb_handle_events_timeout(IntPtr ctx, ref TimeVal tv);
    libusb_error libusb_handle_events_timeout_completed(IntPtr ctx, ref TimeVal tv, IntPtr completed);
    libusb_error libusb_handle_events(IntPtr ctx);
    IntPtr libusb_get_pollfds(IntPtr ctx, out IntPtr pollfds);
    void libusb_free_pollfds(IntPtr pollfds);

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
