using System.Runtime.InteropServices;
using LibUsbNative.Enums;
using LibUsbNative.Structs;

namespace LibUsbNative;

// LibraryImportAttribute not available in .NET6, silence warning until removal of .NET6 support
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

/// <summary>Concrete ILibUsbApi using direct DllImports.</summary>
public sealed class PInvokeLibUsbApi : ILibUsbApi
{
    private const string Lib = "libusb-1.0";

    #region Context/Options

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_init(out IntPtr ctx);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_exit(IntPtr ctx);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_set_option(IntPtr ctx, int option, int value);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_set_option(IntPtr ctx, int option, IntPtr value);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_handle_events_completed(IntPtr ctx, IntPtr completed);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_interrupt_event_handler(IntPtr ctx);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_set_debug(IntPtr ctx, int level);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr libusb_get_version();

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_has_capability(uint capability);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr libusb_strerror(libusb_error errcode);

    #endregion

    #region Device list/refs

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_get_device_list(IntPtr ctx, out IntPtr list);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_free_device_list(IntPtr list, int unrefDevices);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr libusb_ref_device(IntPtr dev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_unref_device(IntPtr dev);

    #endregion

    #region Device metadata

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_get_device_descriptor(IntPtr dev, out native_libusb_device_descriptor d);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_get_active_config_descriptor(IntPtr dev, out IntPtr cfg);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_get_config_descriptor(IntPtr dev, ushort index, out IntPtr cfg);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_free_config_descriptor(IntPtr cfg);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern byte libusb_get_bus_number(IntPtr dev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern byte libusb_get_device_address(IntPtr dev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern byte libusb_get_port_number(IntPtr dev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_get_port_numbers(IntPtr dev, byte[] arr, int len);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr libusb_get_parent(IntPtr dev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_get_device_speed(IntPtr dev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_get_max_packet_size(IntPtr dev, byte endpoint);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_get_max_iso_packet_size(IntPtr dev, byte endpoint);

    #endregion

    #region Open/close


    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_open(IntPtr dev, out IntPtr handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_close(IntPtr handle);

    #endregion

    #region Config/Interfaces

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_set_configuration(IntPtr h, int cfg);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_get_configuration(IntPtr h, out int cfg);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_claim_interface(IntPtr h, int iface);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_release_interface(IntPtr h, int iface);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_set_interface_alt_setting(IntPtr h, int iface, int alt);

    #endregion

    #region Kernel driver

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_kernel_driver_active(IntPtr h, int iface);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_detach_kernel_driver(IntPtr h, int iface);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_attach_kernel_driver(IntPtr h, int iface);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_set_auto_detach_kernel_driver(IntPtr h, int enable);

    #endregion

    #region Strings

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_get_string_descriptor_ascii(IntPtr h, byte idx, byte[] data, int len);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_get_string_descriptor(
        IntPtr h,
        byte idx,
        ushort lang,
        byte[] data,
        int len
    );

    #endregion

    #region Sync I/O

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_control_transfer(
        IntPtr h,
        byte bm,
        byte bReq,
        ushort wVal,
        ushort wIdx,
        byte[] data,
        ushort wLen,
        uint timeout
    );

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_bulk_transfer(
        IntPtr h,
        byte ep,
        byte[] data,
        int len,
        out int xfer,
        uint timeout
    );

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_interrupt_transfer(
        IntPtr h,
        byte ep,
        byte[] data,
        int len,
        out int xfer,
        uint timeout
    );

    #endregion

    #region Halt/Reset

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_clear_halt(IntPtr h, byte ep);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_reset_device(IntPtr h);

    #endregion

    #region Events/Async

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_handle_events_timeout(IntPtr ctx, ref libusb_timeval tv);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_handle_events_timeout_completed(
        IntPtr ctx,
        ref libusb_timeval tv,
        IntPtr completed
    );

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_handle_events(IntPtr ctx);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr libusb_get_pollfds(IntPtr ctx, out IntPtr pollfds);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_free_pollfds(IntPtr pollfds);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr libusb_alloc_transfer(int iso);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_free_transfer(IntPtr t);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_submit_transfer(IntPtr t);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_cancel_transfer(IntPtr t);

    #endregion

    #region Hotplug

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern libusb_error libusb_hotplug_register_callback(
        IntPtr ctx,
        int events,
        int flags,
        int vendor,
        int product,
        int devClass,
        libusb_hotplug_callback_fn cb,
        IntPtr user_data,
        out int callbackHandle
    );

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libusb_hotplug_deregister_callback(IntPtr ctx, IntPtr callbackHandle);

    #endregion

    #region Expose via interface

    libusb_error ILibUsbApi.libusb_init(out IntPtr ctx) => libusb_init(out ctx);

    void ILibUsbApi.libusb_exit(IntPtr ctx) => libusb_exit(ctx);

    libusb_error ILibUsbApi.libusb_set_option(IntPtr ctx, libusb_option option, int value) =>
        libusb_set_option(ctx, (int)option, value);

    libusb_error ILibUsbApi.libusb_set_option(IntPtr ctx, libusb_option option, IntPtr value) =>
        libusb_set_option(ctx, (int)option, value);

    libusb_error ILibUsbApi.libusb_handle_events_completed(IntPtr ctx, IntPtr completed) =>
        (libusb_error)libusb_handle_events_completed(ctx, completed);

    void ILibUsbApi.libusb_interrupt_event_handler(IntPtr ctx) => libusb_interrupt_event_handler(ctx);

    void ILibUsbApi.libusb_set_debug(IntPtr ctx, int level) => libusb_set_debug(ctx, level);

    IntPtr ILibUsbApi.libusb_get_version() => libusb_get_version();

    int ILibUsbApi.libusb_has_capability(uint capability) => libusb_has_capability(capability);

    IntPtr ILibUsbApi.libusb_strerror(libusb_error errorCode) => libusb_strerror(errorCode);

    libusb_error ILibUsbApi.libusb_get_device_list(IntPtr ctx, out IntPtr list) =>
        libusb_get_device_list(ctx, out list);

    void ILibUsbApi.libusb_free_device_list(IntPtr list, int unrefDevices) =>
        libusb_free_device_list(list, unrefDevices);

    void ILibUsbApi.libusb_ref_device(IntPtr dev) => libusb_ref_device(dev);

    void ILibUsbApi.libusb_unref_device(IntPtr dev) => libusb_unref_device(dev);

    libusb_error ILibUsbApi.libusb_get_device_descriptor(IntPtr dev, out native_libusb_device_descriptor d) =>
        libusb_get_device_descriptor(dev, out d);

    libusb_error ILibUsbApi.libusb_get_active_config_descriptor(IntPtr dev, out IntPtr cfg) =>
        libusb_get_active_config_descriptor(dev, out cfg);

    libusb_error ILibUsbApi.libusb_get_config_descriptor(IntPtr dev, ushort index, out IntPtr cfg) =>
        libusb_get_config_descriptor(dev, index, out cfg);

    void ILibUsbApi.libusb_free_config_descriptor(IntPtr cfg) => libusb_free_config_descriptor(cfg);

    byte ILibUsbApi.libusb_get_bus_number(IntPtr dev) => libusb_get_bus_number(dev);

    byte ILibUsbApi.libusb_get_device_address(IntPtr dev) => libusb_get_device_address(dev);

    byte ILibUsbApi.libusb_get_port_number(IntPtr dev) => libusb_get_port_number(dev);

    int ILibUsbApi.libusb_get_port_numbers(IntPtr dev, byte[] arr, int len) => libusb_get_port_numbers(dev, arr, len);

    IntPtr ILibUsbApi.libusb_get_parent(IntPtr dev) => libusb_get_parent(dev);

    int ILibUsbApi.libusb_get_device_speed(IntPtr dev) => libusb_get_device_speed(dev);

    int ILibUsbApi.libusb_get_max_packet_size(IntPtr dev, byte endpoint) => libusb_get_max_packet_size(dev, endpoint);

    int ILibUsbApi.libusb_get_max_iso_packet_size(IntPtr dev, byte endpoint) =>
        libusb_get_max_iso_packet_size(dev, endpoint);

    libusb_error ILibUsbApi.libusb_open(IntPtr dev, out IntPtr h) => libusb_open(dev, out h);

    void ILibUsbApi.libusb_close(IntPtr h) => libusb_close(h);

    libusb_error ILibUsbApi.libusb_set_configuration(IntPtr h, int cfg) => libusb_set_configuration(h, cfg);

    libusb_error ILibUsbApi.libusb_get_configuration(IntPtr h, out int cfg) => libusb_get_configuration(h, out cfg);

    libusb_error ILibUsbApi.libusb_claim_interface(IntPtr h, int i) => libusb_claim_interface(h, i);

    libusb_error ILibUsbApi.libusb_release_interface(IntPtr h, int i) => libusb_release_interface(h, i);

    libusb_error ILibUsbApi.libusb_set_interface_alt_setting(IntPtr h, int i, int alt) =>
        libusb_set_interface_alt_setting(h, i, alt);

    libusb_error ILibUsbApi.libusb_kernel_driver_active(IntPtr h, int i) => libusb_kernel_driver_active(h, i);

    libusb_error ILibUsbApi.libusb_detach_kernel_driver(IntPtr h, int i) => libusb_detach_kernel_driver(h, i);

    libusb_error ILibUsbApi.libusb_attach_kernel_driver(IntPtr h, int i) => libusb_attach_kernel_driver(h, i);

    libusb_error ILibUsbApi.libusb_set_auto_detach_kernel_driver(IntPtr h, int e) =>
        libusb_set_auto_detach_kernel_driver(h, e);

    libusb_error ILibUsbApi.libusb_get_string_descriptor_ascii(IntPtr h, byte idx, byte[] data, int len) =>
        libusb_get_string_descriptor_ascii(h, idx, data, len);

    libusb_error ILibUsbApi.libusb_get_string_descriptor(IntPtr h, byte idx, ushort lang, byte[] data, int len) =>
        libusb_get_string_descriptor(h, idx, lang, data, len);

    libusb_error ILibUsbApi.libusb_control_transfer(
        IntPtr h,
        byte bm,
        byte bReq,
        ushort wVal,
        ushort wIdx,
        byte[] data,
        ushort wLen,
        uint timeout
    ) => libusb_control_transfer(h, bm, bReq, wVal, wIdx, data, wLen, timeout);

    libusb_error ILibUsbApi.libusb_bulk_transfer(IntPtr h, byte ep, byte[] data, int len, out int xfer, uint timeout) =>
        libusb_bulk_transfer(h, ep, data, len, out xfer, timeout);

    libusb_error ILibUsbApi.libusb_interrupt_transfer(
        IntPtr h,
        byte ep,
        byte[] data,
        int len,
        out int xfer,
        uint timeout
    ) => libusb_interrupt_transfer(h, ep, data, len, out xfer, timeout);

    libusb_error ILibUsbApi.libusb_clear_halt(IntPtr h, byte ep) => libusb_clear_halt(h, ep);

    libusb_error ILibUsbApi.libusb_reset_device(IntPtr h) => libusb_reset_device(h);

    libusb_error ILibUsbApi.libusb_handle_events_timeout(IntPtr ctx, ref libusb_timeval tv) =>
        libusb_handle_events_timeout(ctx, ref tv);

    libusb_error ILibUsbApi.libusb_handle_events_timeout_completed(
        IntPtr ctx,
        ref libusb_timeval tv,
        IntPtr completed
    ) => libusb_handle_events_timeout_completed(ctx, ref tv, completed);

    libusb_error ILibUsbApi.libusb_handle_events(IntPtr ctx) => libusb_handle_events(ctx);

    IntPtr ILibUsbApi.libusb_get_pollfds(IntPtr ctx, out IntPtr pollfds) => libusb_get_pollfds(ctx, out pollfds);

    void ILibUsbApi.libusb_free_pollfds(IntPtr pollfds) => libusb_free_pollfds(pollfds);

    IntPtr ILibUsbApi.libusb_alloc_transfer(int iso) => libusb_alloc_transfer(iso);

    void ILibUsbApi.libusb_free_transfer(IntPtr t) => libusb_free_transfer(t);

    libusb_error ILibUsbApi.libusb_submit_transfer(IntPtr t) => libusb_submit_transfer(t);

    libusb_error ILibUsbApi.libusb_cancel_transfer(IntPtr t) => libusb_cancel_transfer(t);

    libusb_error ILibUsbApi.libusb_hotplug_register_callback(
        IntPtr ctx,
        int events,
        int flags,
        int vendorId,
        int productId,
        int devClass,
        libusb_hotplug_callback_fn cb,
        IntPtr user_data,
        out int handle
    ) => libusb_hotplug_register_callback(ctx, events, flags, vendorId, productId, devClass, cb, user_data, out handle);

    void ILibUsbApi.libusb_hotplug_deregister_callback(IntPtr ctx, IntPtr handle) =>
        libusb_hotplug_deregister_callback(ctx, handle);

    #endregion
}

#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
