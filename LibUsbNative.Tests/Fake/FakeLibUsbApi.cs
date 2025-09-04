using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibUsbNative;

namespace LibUsbNative.Tests.Fakes;

internal sealed class FakeLibusbApi : ILibUsbApi
{
    // Simple in-memory world:
    internal native_libusb_device_descriptor Device = new()
    {
        bLength = 18,
        bDescriptorType = 1,
        bcdUSB = 0x0200,
        bDeviceClass = 0xEF,
        bDeviceSubClass = 0x02,
        bDeviceProtocol = 0x01,
        bMaxPacketSize0 = 64,
        idVendor = 0x1234,
        idProduct = 0x5678,
        bcdDevice = 0x0100,
        iManufacturer = 1,
        iProduct = 2,
        iSerialNumber = 3,
        bNumConfigurations = 1,
    };

    public byte[] ManufacturerUtf16 = MakeUtf16String("Acme Inc.");
    public byte[] ProductUtf16 = MakeUtf16String("USB Gizmo");
    public byte[] SerialAscii = System.Text.Encoding.ASCII.GetBytes("SN123456");

    public byte[] LangIdx0 = new byte[] { 4, 3, 0x09, 0x04 }; // length=4, type=3, LANGID 0x0409

    public static byte[] MakeUtf16String(string s)
    {
        var payload = System.Text.Encoding.Unicode.GetBytes(s);
        var buf = new byte[payload.Length + 2];
        buf[0] = (byte)(buf.Length);
        buf[1] = 3; // string desc
        Array.Copy(payload, 0, buf, 2, payload.Length);
        return buf;
    }

    // Device list memory
    private readonly List<IntPtr> _devices = new() { new IntPtr(0x1000) };

    // Hotplug
    public libusb_hotplug_callback_fn? LastCb;
    public int LastCbHandle = 42;

    // Context/Options
    public LibUsbError libusb_init(out IntPtr ctx)
    {
        ctx = new IntPtr(0xDEADBEEF);
        return 0;
    }

    public void libusb_exit(IntPtr ctx) { }

    public LibUsbError libusb_set_option(IntPtr ctx, LibusbOption option, IntPtr value) => 0;

    public LibUsbError libusb_handle_events_completed(IntPtr ctx, IntPtr completed) => 0;

    public void libusb_interrupt_event_handler(IntPtr ctx) { }

    public void libusb_set_debug(IntPtr ctx, int level) { }

    public IntPtr libusb_get_version() => IntPtr.Zero;

    public int libusb_has_capability(uint capability) => 1;

    public IntPtr libusb_strerror(LibUsbError errorCode) => IntPtr.Zero;

    // Device list
    public LibUsbError libusb_get_device_list(IntPtr ctx, out IntPtr list)
    {
        // Simulate an array of pointers; we just hand back a pointer and count in return value.
        // We'll store count in the returned IntPtr and list as a fake base address.
        list = new IntPtr(0x2000);
        return (LibUsbError)_devices.Count;
    }

    public void libusb_free_device_list(IntPtr list, int unrefDevices) { }

    public void libusb_ref_device(IntPtr dev) { }

    public void libusb_unref_device(IntPtr dev) { }

    // Device metadata
    public LibUsbError libusb_get_device_descriptor(IntPtr dev, out native_libusb_device_descriptor desc)
    {
        desc = Device;
        return 0;
    }

    public LibUsbError libusb_get_active_config_descriptor(IntPtr dev, out IntPtr config)
    {
        config = IntPtr.Zero;
        return LibUsbError.NotFound;
    }

    public LibUsbError libusb_get_config_descriptor(IntPtr dev, ushort index, out IntPtr config)
    {
        config = IntPtr.Zero;
        return LibUsbError.NotFound;
    }

    public void libusb_free_config_descriptor(IntPtr config) { }

    public byte libusb_get_bus_number(IntPtr dev) => 3;

    public byte libusb_get_device_address(IntPtr dev) => 17;

    public byte libusb_get_port_number(IntPtr dev) => 1;

    public int libusb_get_port_numbers(IntPtr dev, byte[] port_numbers, int len)
    {
        if (len > 0)
            port_numbers[0] = 1;
        return 1;
    }

    public IntPtr libusb_get_parent(IntPtr dev) => IntPtr.Zero;

    public int libusb_get_device_speed(IntPtr dev) => (int)LibusbSpeed.High;

    public int libusb_get_max_packet_size(IntPtr dev, byte endpoint) => 512;

    public int libusb_get_max_iso_packet_size(IntPtr dev, byte ep) => 1024;

    // Open/close
    public LibUsbError libusb_open(IntPtr dev, out IntPtr handle)
    {
        handle = new IntPtr(0x3000);
        return 0;
    }

    public void libusb_close(IntPtr handle) { }

    // Config/Interface
    public LibUsbError libusb_set_configuration(IntPtr handle, int configuration) => 0;

    public LibUsbError libusb_get_configuration(IntPtr handle, out int configuration)
    {
        configuration = 1;
        return 0;
    }

    public LibUsbError libusb_claim_interface(IntPtr handle, int interfaceNumber) => 0;

    public LibUsbError libusb_release_interface(IntPtr handle, int interfaceNumber) => 0;

    public LibUsbError libusb_set_interface_alt_setting(IntPtr handle, int interfaceNumber, int alternateSetting) => 0;

    // Kernel driver
    public LibUsbError libusb_kernel_driver_active(IntPtr handle, int interfaceNumber) => 0;

    public LibUsbError libusb_detach_kernel_driver(IntPtr handle, int interfaceNumber) => 0;

    public LibUsbError libusb_attach_kernel_driver(IntPtr handle, int interfaceNumber) => 0;

    public LibUsbError libusb_set_auto_detach_kernel_driver(IntPtr handle, int enable) => 0;

    // Strings
    public LibUsbError libusb_get_string_descriptor_ascii(IntPtr h, byte idx, byte[] data, int length)
    {
        if (idx == 3)
        {
            var n = Math.Min(length, SerialAscii.Length);
            Array.Copy(SerialAscii, data, n);
            return (LibUsbError)n;
        }
        return LibUsbError.NotFound;
    }

    public LibUsbError libusb_get_string_descriptor(IntPtr h, byte idx, ushort langid, byte[] data, int length)
    {
        byte[] src = idx switch
        {
            0 => LangIdx0,
            1 => ManufacturerUtf16,
            2 => ProductUtf16,
            _ => Array.Empty<byte>(),
        };
        if (src.Length == 0)
            return LibUsbError.NotFound;

        var n = Math.Min(length, src.Length);
        Array.Copy(src, data, n);
        return (LibUsbError)n;
    }

    // Sync I/O
    public LibUsbError libusb_control_transfer(
        IntPtr handle,
        byte bm,
        byte bReq,
        ushort wVal,
        ushort wIdx,
        byte[] data,
        ushort wLen,
        uint timeout
    ) => 0;

    public LibUsbError libusb_bulk_transfer(IntPtr handle, byte ep, byte[] data, int len, out int xfer, uint timeout)
    {
        if ((ep & 0x80) != 0) // IN
        {
            xfer = Math.Min(len, 4);
            for (int i = 0; i < xfer; i++)
                data[i] = (byte)(0xA0 + i);
            return LibUsbError.Success;
        }
        else
        {
            xfer = len;
            return LibUsbError.Success;
        }
    }

    public LibUsbError libusb_interrupt_transfer(
        IntPtr handle,
        byte ep,
        byte[] data,
        int len,
        out int transferred,
        uint timeout
    )
    {
        transferred = Math.Min(len, 2);
        for (int i = 0; i < transferred; i++)
            data[i] = (byte)(0xB0 + i);
        return LibUsbError.Success;
    }

    // Halt/Reset
    public LibUsbError libusb_clear_halt(IntPtr handle, byte endpoint) => 0;

    public LibUsbError libusb_reset_device(IntPtr handle) => 0;

    // Events / async
    public LibUsbError libusb_handle_events_timeout(IntPtr ctx, ref TimeVal tv) => 0;

    public LibUsbError libusb_handle_events_timeout_completed(IntPtr ctx, ref TimeVal tv, IntPtr completed) => 0;

    public LibUsbError libusb_handle_events(IntPtr ctx) => 0;

    public IntPtr libusb_get_pollfds(IntPtr ctx, out IntPtr pollfds)
    {
        pollfds = IntPtr.Zero;
        return IntPtr.Zero;
    }

    public void libusb_free_pollfds(IntPtr pollfds) { }

    public IntPtr libusb_alloc_transfer(int iso_packets) => new IntPtr(0x4000);

    public void libusb_free_transfer(IntPtr transfer) { }

    public LibUsbError libusb_submit_transfer(IntPtr transfer) => 0;

    public LibUsbError libusb_cancel_transfer(IntPtr transfer) => 0;

    public LibUsbError libusb_hotplug_register_callback(
        IntPtr ctx,
        int events,
        int flags,
        int vendorId,
        int productId,
        int devClass,
        libusb_hotplug_callback_fn cb,
        IntPtr user_data,
        out int callbackHandle
    )
    {
        LastCb = cb;
        callbackHandle = LastCbHandle;
        return LibUsbError.Success;
    }

    public void libusb_hotplug_deregister_callback(IntPtr ctx, IntPtr callbackHandle) { }

    // Helper to simulate events:
    public void FireHotplugArrived(IntPtr ctx, IntPtr dev) => LastCb?.Invoke(ctx, dev, 0x01, IntPtr.Zero);

    public void FireHotplugLeft(IntPtr ctx, IntPtr dev) => LastCb?.Invoke(ctx, dev, 0x02, IntPtr.Zero);

    /*
    public static implicit operator ILibUsbApi(FakeLibusbApi v)
    {
        throw new NotImplementedException();
    }
    */
}
