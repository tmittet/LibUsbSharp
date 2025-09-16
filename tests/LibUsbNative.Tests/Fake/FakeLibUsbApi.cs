using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using LibUsbNative;
using LibUsbNative.Descriptors;

namespace LibUsbNative.Tests.Fakes;

/// <summary>
/// Enhanced in-memory fake of libusb for tests.
/// Provides:
/// - Deterministic mocked data for every API call
/// - Realistic unmanaged allocations for device lists & configuration descriptors
/// - Error injection per API (first-in-first-consumed)
/// - Proper unmanaged resource cleanup (IDisposable + finalizer)
/// </summary>
internal sealed class FakeLibusbApi : ILibUsbApi, IDisposable
{
    // ---------------------------
    // Simple in-memory device model
    // ---------------------------
    internal native_libusb_device_descriptor Device = new()
    {
        bLength = 18,
        bDescriptorType = 1,
        bcdUSB = 0x0200,
        bDeviceClass = 0xEF, // Misc
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

    // String descriptors (index 0: LANGID table)
    public byte[] ManufacturerUtf16 = MakeUtf16String("Acme Inc.");
    public byte[] ProductUtf16 = MakeUtf16String("USB Gizmo");
    public byte[] SerialAscii = Encoding.ASCII.GetBytes("SN123456");
    public byte[] LangIdx0 = { 4, 3, 0x09, 0x04 }; // English (US)

    public static byte[] MakeUtf16String(string s)
    {
        var payload = Encoding.Unicode.GetBytes(s);
        var buf = new byte[payload.Length + 2];
        buf[0] = (byte)buf.Length;
        buf[1] = 3; // USB string descriptor type
        Array.Copy(payload, 0, buf, 2, payload.Length);
        return buf;
    }

    // Device list: single fake device pointer
    private readonly List<IntPtr> _devices = new() { new IntPtr(0x1000) };

    // Allocations we must free
    private readonly List<IntPtr> _deviceListBlocks = new();
    private readonly List<ConfigAllocation> _configAllocs = new();

    private record struct ConfigAllocation(
        IntPtr Config,
        IntPtr InterfaceArray,
        IntPtr AltSettingArray,
        IntPtr EndpointArray
    );

    // Hotplug
    public libusb_hotplug_callback_fn? LastCb;
    public int LastCbHandle = 42;

    // State tracking
    private readonly Dictionary<LibusbOption, IntPtr> _options = new();
    private readonly HashSet<IntPtr> _openHandles = new();
    private readonly HashSet<(IntPtr Handle, int Interface)> _claimed = new();
    private int _nextHandle = 0x3000;
    private int _nextTransfer = 0x4000;

    // Version / strerror
    private readonly IntPtr _versionPtr;
    private readonly IntPtr _versionRcPtr;
    private readonly IntPtr _versionDescPtr;
    private readonly Dictionary<LibUsbError, IntPtr> _strErrorPtrs;

    // Error injection (API name -> queue of factories)
    private readonly Dictionary<string, Queue<Func<LibUsbError>>> _errorInjectors = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    // Disposal
    private bool _disposed;

    public FakeLibusbApi()
    {
        // Allocate version structure + strings once.
        _versionRcPtr = AllocAnsi("mock");
        _versionDescPtr = AllocAnsi("LibUsbSharp Test Fake");

        var verNative = new LibUsbNative.native_libusb_version
        {
            major = 1,
            minor = 0,
            micro = 0,
            nano = 0,
            rc = _versionRcPtr,
            describe = _versionDescPtr,
        };
        _versionPtr = Marshal.AllocHGlobal(Marshal.SizeOf<LibUsbNative.native_libusb_version>());
        Marshal.StructureToPtr(verNative, _versionPtr, false);

        _strErrorPtrs = new();
        foreach (LibUsbError e in Enum.GetValues(typeof(LibUsbError)))
        {
            _strErrorPtrs[e] = AllocAnsi(e.ToString());
        }
    }

    private static IntPtr AllocAnsi(string s)
    {
        var bytes = Encoding.ASCII.GetBytes(s + "\0");
        var p = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, p, bytes.Length);
        return p;
    }

    // ---------------------------
    // Error Injection API
    // ---------------------------
    public void InjectError(string apiName, LibUsbError error) => InjectError(apiName, () => error);

    public void InjectErrors(string apiName, IEnumerable<LibUsbError> errors)
    {
        foreach (var e in errors)
            InjectError(apiName, e);
    }

    public void InjectError(string apiName, Func<LibUsbError> factory)
    {
        lock (_lock)
        {
            if (!_errorInjectors.TryGetValue(apiName, out var q))
            {
                q = new Queue<Func<LibUsbError>>();
                _errorInjectors[apiName] = q;
            }
            q.Enqueue(factory);
        }
    }

    private bool TryConsumeInjected(string name, out LibUsbError error)
    {
        lock (_lock)
        {
            if (_errorInjectors.TryGetValue(name, out var q) && q.Count > 0)
            {
                error = q.Dequeue().Invoke();
                return true;
            }
        }
        error = LibUsbError.Success;
        return false;
    }

    private LibUsbError MaybeFail(string apiName) => TryConsumeInjected(apiName, out var e) ? e : LibUsbError.Success;

    private LibUsbError MaybeFail(string apiName, out LibUsbError error)
    {
        error = MaybeFail(apiName);
        return error;
    }

    // ------------- Context / Options -------------
    public LibUsbError libusb_init(out IntPtr ctx)
    {
        ctx = IntPtr.Zero;
        if (MaybeFail(nameof(libusb_init), out var err) != LibUsbError.Success)
            return err;
        ctx = new IntPtr(0xDEADBEEF);
        return LibUsbError.Success;
    }

    public void libusb_exit(IntPtr ctx) { }

    public LibUsbError libusb_set_option(IntPtr ctx, LibusbOption option, int value)
    {
        if (MaybeFail(nameof(libusb_set_option), out var err) != LibUsbError.Success)
            return err;
        _options[option] = (IntPtr)value;
        return LibUsbError.Success;
    }

    public LibUsbError libusb_set_option(IntPtr ctx, LibusbOption option, IntPtr value)
    {
        if (MaybeFail(nameof(libusb_set_option), out var err) != LibUsbError.Success)
            return err;
        _options[option] = value;
        return LibUsbError.Success;
    }

    public LibUsbError libusb_handle_events_completed(IntPtr ctx, IntPtr completed) =>
        MaybeFail(nameof(libusb_handle_events_completed));

    public void libusb_interrupt_event_handler(IntPtr ctx) { }

    public void libusb_set_debug(IntPtr ctx, int level) { }

    public IntPtr libusb_get_version() => _versionPtr;

    public int libusb_has_capability(uint capability) => 1;

    public IntPtr libusb_strerror(LibUsbError errorCode) =>
        _strErrorPtrs.TryGetValue(errorCode, out var p) ? p : _strErrorPtrs[LibUsbError.Other];

    // ------------- Device list -------------
    public LibUsbError libusb_get_device_list(IntPtr ctx, out IntPtr list)
    {
        list = IntPtr.Zero;
        if (MaybeFail(nameof(libusb_get_device_list), out var err) != LibUsbError.Success)
            return err;

        int count = _devices.Count;
        int total = (count + 1) * IntPtr.Size;
        var block = Marshal.AllocHGlobal(total);
        for (int i = 0; i < count; i++)
            Marshal.WriteIntPtr(block, i * IntPtr.Size, _devices[i]);
        Marshal.WriteIntPtr(block, count * IntPtr.Size, IntPtr.Zero);

        list = block;
        _deviceListBlocks.Add(block);
        return (LibUsbError)count;
    }

    public void libusb_free_device_list(IntPtr list, int unrefDevices)
    {
        if (list != IntPtr.Zero && _deviceListBlocks.Remove(list))
            Marshal.FreeHGlobal(list);
    }

    public void libusb_ref_device(IntPtr dev) { }

    public void libusb_unref_device(IntPtr dev) { }

    // ------------- Device metadata -------------
    public LibUsbError libusb_get_device_descriptor(IntPtr dev, out native_libusb_device_descriptor desc)
    {
        desc = default;
        if (MaybeFail(nameof(libusb_get_device_descriptor), out var err) != LibUsbError.Success)
            return err;
        desc = Device;
        return LibUsbError.Success;
    }

    public LibUsbError libusb_get_active_config_descriptor(IntPtr dev, out IntPtr config) =>
        libusb_get_config_descriptor(dev, 0, out config);

    public LibUsbError libusb_get_config_descriptor(IntPtr dev, ushort index, out IntPtr config)
    {
        config = IntPtr.Zero;
        if (MaybeFail(nameof(libusb_get_config_descriptor), out var err) != LibUsbError.Success)
            return err;
        if (index != 0)
            return LibUsbError.NotFound;

        // 2 endpoints: EP1 OUT bulk, EP1 IN bulk
        var epCount = 2;
        var epSize = Marshal.SizeOf<native_libusb_endpoint_descriptor>();
        var epPtr = Marshal.AllocHGlobal(epCount * epSize);

        void WriteEndpoint(int offset, byte address, byte attrs, ushort maxPacket, byte interval)
        {
            var ep = new native_libusb_endpoint_descriptor
            {
                bLength = 7,
                bDescriptorType = (byte)UsbDescriptorType.Endpoint,
                bEndpointAddress = address,
                bmAttributes = attrs,
                wMaxPacketSize = maxPacket,
                bInterval = interval,
                bRefresh = 0,
                bSynchAddress = 0,
                extra = IntPtr.Zero,
                extra_length = 0,
            };
            Marshal.StructureToPtr(ep, IntPtr.Add(epPtr, offset * epSize), false);
        }

        WriteEndpoint(0, 0x01, 0x02, 512, 0);
        WriteEndpoint(1, 0x81, 0x02, 512, 0);

        var ifDesc = new native_libusb_interface_descriptor
        {
            bLength = 9,
            bDescriptorType = (byte)UsbDescriptorType.Interface,
            bInterfaceNumber = 0,
            bAlternateSetting = 0,
            bNumEndpoints = (byte)epCount,
            bInterfaceClass = (byte)UsbClass.Miscellaneous,
            bInterfaceSubClass = 0x02,
            bInterfaceProtocol = 0x01,
            iInterface = 0,
            endpoint = epPtr,
            extra = IntPtr.Zero,
            extra_length = 0,
        };
        var ifDescPtr = Marshal.AllocHGlobal(Marshal.SizeOf<native_libusb_interface_descriptor>());
        Marshal.StructureToPtr(ifDesc, ifDescPtr, false);

        var nativeInterface = new native_libusb_interface { altsetting = ifDescPtr, num_altsetting = 1 };
        var interfaceArrayPtr = Marshal.AllocHGlobal(Marshal.SizeOf<native_libusb_interface>());
        Marshal.StructureToPtr(nativeInterface, interfaceArrayPtr, false);

        ushort totalLength = (ushort)(
            Marshal.SizeOf<native_libusb_config_descriptor>()
            + Marshal.SizeOf<native_libusb_interface>()
            + Marshal.SizeOf<native_libusb_interface_descriptor>()
            + epCount * Marshal.SizeOf<native_libusb_endpoint_descriptor>()
        );

        var cfg = new native_libusb_config_descriptor
        {
            bLength = 9,
            bDescriptorType = (byte)UsbDescriptorType.Configuration,
            wTotalLength = totalLength,
            bNumInterfaces = 1,
            bConfigurationValue = 1,
            iConfiguration = 0,
            bmAttributes = (byte)(UsbConfigAttributes.MustBeSet | UsbConfigAttributes.SelfPowered),
            MaxPower = 50,
            interfacePtr = interfaceArrayPtr,
            extra = IntPtr.Zero,
            extra_length = 0,
        };
        var cfgPtr = Marshal.AllocHGlobal(Marshal.SizeOf<native_libusb_config_descriptor>());
        Marshal.StructureToPtr(cfg, cfgPtr, false);

        _configAllocs.Add(new ConfigAllocation(cfgPtr, interfaceArrayPtr, ifDescPtr, epPtr));

        config = cfgPtr;
        return LibUsbError.Success;
    }

    public void libusb_free_config_descriptor(IntPtr config)
    {
        for (int i = 0; i < _configAllocs.Count; i++)
        {
            if (_configAllocs[i].Config == config)
            {
                var alloc = _configAllocs[i];
                Marshal.FreeHGlobal(alloc.EndpointArray);
                Marshal.FreeHGlobal(alloc.AltSettingArray);
                Marshal.FreeHGlobal(alloc.InterfaceArray);
                Marshal.FreeHGlobal(alloc.Config);
                _configAllocs.RemoveAt(i);
                break;
            }
        }
    }

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

    // ------------- Open / close -------------
    public LibUsbError libusb_open(IntPtr dev, out IntPtr handle)
    {
        handle = IntPtr.Zero;
        if (MaybeFail(nameof(libusb_open), out var err) != LibUsbError.Success)
            return err;
        handle = new IntPtr(_nextHandle++);
        _openHandles.Add(handle);
        return LibUsbError.Success;
    }

    public void libusb_close(IntPtr handle)
    {
        _openHandles.Remove(handle);
        _claimed.RemoveWhere(c => c.Handle == handle);
    }

    // ------------- Config / Interface -------------
    public LibUsbError libusb_set_configuration(IntPtr handle, int configuration) =>
        MaybeFail(nameof(libusb_set_configuration));

    public LibUsbError libusb_get_configuration(IntPtr handle, out int configuration)
    {
        configuration = 0;
        if (MaybeFail(nameof(libusb_get_configuration), out var err) != LibUsbError.Success)
            return err;
        configuration = 1;
        return LibUsbError.Success;
    }

    public LibUsbError libusb_claim_interface(IntPtr handle, int interfaceNumber)
    {
        var err = MaybeFail(nameof(libusb_claim_interface));
        if (err == LibUsbError.Success)
            _claimed.Add((handle, interfaceNumber));
        return err;
    }

    public LibUsbError libusb_release_interface(IntPtr handle, int interfaceNumber)
    {
        var err = MaybeFail(nameof(libusb_release_interface));
        if (err == LibUsbError.Success)
            _claimed.Remove((handle, interfaceNumber));
        return err;
    }

    public LibUsbError libusb_set_interface_alt_setting(IntPtr handle, int interfaceNumber, int alternateSetting) =>
        MaybeFail(nameof(libusb_set_interface_alt_setting));

    // ------------- Kernel driver -------------
    public LibUsbError libusb_kernel_driver_active(IntPtr handle, int interfaceNumber) =>
        MaybeFail(nameof(libusb_kernel_driver_active));

    public LibUsbError libusb_detach_kernel_driver(IntPtr handle, int interfaceNumber) =>
        MaybeFail(nameof(libusb_detach_kernel_driver));

    public LibUsbError libusb_attach_kernel_driver(IntPtr handle, int interfaceNumber) =>
        MaybeFail(nameof(libusb_attach_kernel_driver));

    public LibUsbError libusb_set_auto_detach_kernel_driver(IntPtr handle, int enable) =>
        MaybeFail(nameof(libusb_set_auto_detach_kernel_driver));

    // ------------- Strings -------------
    public LibUsbError libusb_get_string_descriptor_ascii(IntPtr h, byte idx, byte[] data, int length)
    {
        if (MaybeFail(nameof(libusb_get_string_descriptor_ascii), out var err) != LibUsbError.Success)
            return err;

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
        if (MaybeFail(nameof(libusb_get_string_descriptor), out var err) != LibUsbError.Success)
            return err;

        byte[] src = idx switch
        {
            0 => LangIdx0,
            1 => ManufacturerUtf16,
            2 => ProductUtf16,
            3 => MakeUtf16String("SN123456"),
            _ => Array.Empty<byte>(),
        };
        if (src.Length == 0)
            return LibUsbError.NotFound;

        var n = Math.Min(length, src.Length);
        Array.Copy(src, data, n);
        return (LibUsbError)n;
    }

    // ------------- Sync I/O -------------
    public LibUsbError libusb_control_transfer(
        IntPtr handle,
        byte bm,
        byte bReq,
        ushort wVal,
        ushort wIdx,
        byte[] data,
        ushort wLen,
        uint timeout
    )
    {
        if (MaybeFail(nameof(libusb_control_transfer), out var err) != LibUsbError.Success)
            return err;

        if ((bm & 0x80) != 0 && data is not null)
        {
            int count = Math.Min(wLen, (ushort)data.Length);
            for (int i = 0; i < count; i++)
                data[i] = (byte)((bReq + i) & 0xFF);
            return (LibUsbError)count;
        }
        return (LibUsbError)wLen;
    }

    public LibUsbError libusb_bulk_transfer(
        IntPtr handle,
        byte ep,
        byte[] data,
        int len,
        out int transferred,
        uint timeout
    )
    {
        transferred = 0;
        if (MaybeFail(nameof(libusb_bulk_transfer), out var err) != LibUsbError.Success)
            return err;

        if ((ep & 0x80) != 0)
        {
            transferred = Math.Min(len, 8);
            for (int i = 0; i < transferred; i++)
                data[i] = (byte)(0xA0 + i);
        }
        else
        {
            transferred = len;
        }
        return LibUsbError.Success;
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
        transferred = 0;
        if (MaybeFail(nameof(libusb_interrupt_transfer), out var err) != LibUsbError.Success)
            return err;

        transferred = Math.Min(len, 4);
        for (int i = 0; i < transferred; i++)
            data[i] = (byte)(0xB0 + i);
        return LibUsbError.Success;
    }

    // ------------- Halt / Reset -------------
    public LibUsbError libusb_clear_halt(IntPtr handle, byte endpoint) => MaybeFail(nameof(libusb_clear_halt));

    public LibUsbError libusb_reset_device(IntPtr handle) => MaybeFail(nameof(libusb_reset_device));

    // ------------- Events / async -------------
    public LibUsbError libusb_handle_events_timeout(IntPtr ctx, ref TimeVal tv) =>
        MaybeFail(nameof(libusb_handle_events_timeout));

    public LibUsbError libusb_handle_events_timeout_completed(IntPtr ctx, ref TimeVal tv, IntPtr completed) =>
        MaybeFail(nameof(libusb_handle_events_timeout_completed));

    public LibUsbError libusb_handle_events(IntPtr ctx) => MaybeFail(nameof(libusb_handle_events));

    public IntPtr libusb_get_pollfds(IntPtr ctx, out IntPtr pollfds)
    {
        pollfds = IntPtr.Zero;
        return IntPtr.Zero;
    }

    public void libusb_free_pollfds(IntPtr pollfds) { }

    // ------------- Transfers -------------
    public IntPtr libusb_alloc_transfer(int iso_packets) => new(_nextTransfer++);

    public void libusb_free_transfer(IntPtr transfer) { }

    public LibUsbError libusb_submit_transfer(IntPtr transfer) => MaybeFail(nameof(libusb_submit_transfer));

    public LibUsbError libusb_cancel_transfer(IntPtr transfer) => MaybeFail(nameof(libusb_cancel_transfer));

    // ------------- Hotplug -------------
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
        callbackHandle = 0;
        if (MaybeFail(nameof(libusb_hotplug_register_callback), out var err) != LibUsbError.Success)
            return err;

        LastCb = cb;
        callbackHandle = LastCbHandle;
        return LibUsbError.Success;
    }

    public void libusb_hotplug_deregister_callback(IntPtr ctx, IntPtr callbackHandle)
    {
        if ((int)callbackHandle == LastCbHandle)
            LastCb = null;
    }

    public void FireHotplugArrived(IntPtr ctx, IntPtr dev) => LastCb?.Invoke(ctx, dev, 0x01, IntPtr.Zero);

    public void FireHotplugLeft(IntPtr ctx, IntPtr dev) => LastCb?.Invoke(ctx, dev, 0x02, IntPtr.Zero);

    // ---------------------------
    // Disposal
    // ---------------------------
    private void FreeAllResources()
    {
        if (_disposed)
            return;
        _disposed = true;

        foreach (var block in _deviceListBlocks)
        {
            if (block != IntPtr.Zero)
                Marshal.FreeHGlobal(block);
        }
        _deviceListBlocks.Clear();

        foreach (var cfg in _configAllocs)
        {
            if (cfg.EndpointArray != IntPtr.Zero)
                Marshal.FreeHGlobal(cfg.EndpointArray);
            if (cfg.AltSettingArray != IntPtr.Zero)
                Marshal.FreeHGlobal(cfg.AltSettingArray);
            if (cfg.InterfaceArray != IntPtr.Zero)
                Marshal.FreeHGlobal(cfg.InterfaceArray);
            if (cfg.Config != IntPtr.Zero)
                Marshal.FreeHGlobal(cfg.Config);
        }
        _configAllocs.Clear();

        foreach (var kv in _strErrorPtrs)
        {
            if (kv.Value != IntPtr.Zero)
                Marshal.FreeHGlobal(kv.Value);
        }
        _strErrorPtrs.Clear();

        if (_versionPtr != IntPtr.Zero)
            Marshal.FreeHGlobal(_versionPtr);
        if (_versionRcPtr != IntPtr.Zero)
            Marshal.FreeHGlobal(_versionRcPtr);
        if (_versionDescPtr != IntPtr.Zero)
            Marshal.FreeHGlobal(_versionDescPtr);
    }

    ~FakeLibusbApi() => FreeAllResources();

    public void Dispose()
    {
        FreeAllResources();
        GC.SuppressFinalize(this);
    }
}
