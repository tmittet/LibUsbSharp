using System.Runtime.InteropServices;
using LibUsbSharp.Native.Enums;
using LibUsbSharp.Native.Structs;

namespace LibUsbSharp.Native.SafeHandles;

internal sealed class SafeDevice : SafeHandle, ISafeDevice
{
    private readonly SafeContext _context;

    internal ILibUsbApi Api => _context.Api;

    public override bool IsInvalid => handle == IntPtr.Zero;

    public SafeDevice(SafeContext context, nint devicePtr)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        if (devicePtr == IntPtr.Zero)
            throw new ArgumentNullException(nameof(devicePtr));

        _context = context;
        _context.Api.libusb_ref_device(devicePtr);
        handle = devicePtr;
    }

    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
            return true;

        _context.Api.libusb_unref_device(handle);
        _context.DangerousRelease();
        return true;
    }

    /// <inheritdoc />
    public libusb_device_descriptor GetDeviceDescriptor()
    {
        SafeHelper.ThrowIfClosed(this);

        var result = _context.Api.libusb_get_device_descriptor(handle, out var descriptor);
        LibUsbException.ThrowIfApiError(result, nameof(_context.Api.libusb_get_device_descriptor));
        return descriptor;
    }

    /// <inheritdoc />
    public ISafeConfigDescriptor GetActiveConfigDescriptorPtr()
    {
        SafeHelper.ThrowIfClosed(this);

        var result = _context.Api.libusb_get_active_config_descriptor(handle, out var descriptor);
        LibUsbException.ThrowIfApiError(result, nameof(_context.Api.libusb_get_active_config_descriptor));

        var success = false;
        DangerousAddRef(ref success);
        if (!success)
        {
            _context.Api.libusb_free_config_descriptor(descriptor);
            throw LibUsbException.FromError(libusb_error.LIBUSB_ERROR_OTHER, "Failed to ref SafeHandle.");
        }

        return new SafeConfigDescriptor(this, descriptor);
    }

    /// <inheritdoc />
    public libusb_config_descriptor GetActiveConfigDescriptor()
    {
        SafeHelper.ThrowIfClosed(this);

        var result = _context.Api.libusb_get_active_config_descriptor(handle, out var descriptor);
        LibUsbException.ThrowIfApiError(result, nameof(_context.Api.libusb_get_active_config_descriptor));
        try
        {
            var config = FromPointer(descriptor);
            return config;
        }
        finally
        {
            _context.Api.libusb_free_config_descriptor(descriptor);
        }
    }

    /// <inheritdoc />
    public ISafeConfigDescriptor GetConfigDescriptorPtr(byte config_index)
    {
        SafeHelper.ThrowIfClosed(this);

        var result = _context.Api.libusb_get_config_descriptor(handle, config_index, out var descriptor);
        LibUsbException.ThrowIfApiError(result, nameof(_context.Api.libusb_get_config_descriptor));

        var success = false;
        DangerousAddRef(ref success);
        if (!success)
        {
            _context.Api.libusb_free_config_descriptor(descriptor);
            throw LibUsbException.FromError(libusb_error.LIBUSB_ERROR_OTHER, "Failed to ref SafeHandle.");
        }

        return new SafeConfigDescriptor(this, descriptor);
    }

    /// <inheritdoc />
    public libusb_config_descriptor GetConfigDescriptor(byte config_index)
    {
        SafeHelper.ThrowIfClosed(this);

        var result = _context.Api.libusb_get_config_descriptor(handle, config_index, out var descriptor);
        LibUsbException.ThrowIfApiError(result, nameof(_context.Api.libusb_get_config_descriptor));
        try
        {
            var config = FromPointer(descriptor);
            return config;
        }
        finally
        {
            _context.Api.libusb_free_config_descriptor(descriptor);
        }
    }

    /// <inheritdoc />
    public byte GetBusNumber()
    {
        SafeHelper.ThrowIfClosed(this);
        return _context.Api.libusb_get_bus_number(handle);
    }

    /// <inheritdoc />
    public byte GetDeviceAddress()
    {
        SafeHelper.ThrowIfClosed(this);
        return _context.Api.libusb_get_device_address(handle);
    }

    /// <inheritdoc />
    public byte GetPortNumber()
    {
        SafeHelper.ThrowIfClosed(this);
        return _context.Api.libusb_get_port_number(handle);
    }

    /// <inheritdoc />
    public ISafeDeviceHandle Open()
    {
        SafeHelper.ThrowIfClosed(this);

        var result = _context.Api.libusb_open(handle, out var deviceHandle);
        LibUsbException.ThrowIfApiError(result, nameof(_context.Api.libusb_open));

        // Ref counter for context incremented here, not the SafeDevice ref counter.
        // This is intentional since the device pointer is "owned" by the context,
        // it's not related to something that was created by this SafeDevice.
        var success = false;
        _context.DangerousAddRef(ref success);
        if (!success)
        {
            _context.Api.libusb_close(deviceHandle);
            throw LibUsbException.FromError(libusb_error.LIBUSB_ERROR_OTHER, "Failed to ref SafeHandle.");
        }
        return new SafeDeviceHandle(_context, deviceHandle, handle);
    }

    private static libusb_config_descriptor FromPointer(nint pConfigDescriptor)
    {
        if (pConfigDescriptor == IntPtr.Zero)
            throw new ArgumentNullException(nameof(pConfigDescriptor));

        var cfg = Marshal.PtrToStructure<native_libusb_config_descriptor>(pConfigDescriptor);

        var interfaces = ReadArray(
            cfg.interfacePtr,
            cfg.bNumInterfaces,
            elemPtr =>
            {
                var nIf = Marshal.PtrToStructure<native_libusb_interface>(elemPtr);

                var alt = ReadArray(
                    nIf.altsetting,
                    nIf.num_altsetting,
                    ifDescPtr =>
                    {
                        var id = Marshal.PtrToStructure<native_libusb_interface_descriptor>(ifDescPtr);

                        var endpoints = ReadArray(
                            id.endpoint,
                            id.bNumEndpoints,
                            epPtr =>
                            {
                                var ep = Marshal.PtrToStructure<native_libusb_endpoint_descriptor>(epPtr);
                                var extraEp = ReadExtra(ep.extra, ep.extra_length);

                                return new libusb_endpoint_descriptor(
                                    ep.bLength,
                                    (libusb_descriptor_type)ep.bDescriptorType,
                                    new libusb_endpoint_address(ep.bEndpointAddress),
                                    new libusb_endpoint_attributes(ep.bmAttributes),
                                    ep.wMaxPacketSize,
                                    ep.bInterval,
                                    ep.bRefresh,
                                    ep.bSynchAddress,
                                    extraEp
                                );
                            },
                            Marshal.SizeOf<native_libusb_endpoint_descriptor>()
                        );

                        var extraIf = ReadExtra(id.extra, id.extra_length);

                        return new libusb_interface_descriptor(
                            id.bLength,
                            (libusb_descriptor_type)id.bDescriptorType,
                            id.bInterfaceNumber,
                            id.bAlternateSetting,
                            id.bNumEndpoints,
                            (libusb_class_code)id.bInterfaceClass,
                            id.bInterfaceSubClass,
                            id.bInterfaceProtocol,
                            id.iInterface,
                            endpoints,
                            extraIf
                        );
                    },
                    Marshal.SizeOf<native_libusb_interface_descriptor>()
                );

                return new libusb_interface(alt);
            },
            Marshal.SizeOf<native_libusb_interface>()
        );

        var extraCfg = ReadExtra(cfg.extra, cfg.extra_length);

        return new libusb_config_descriptor(
            cfg.bLength,
            (libusb_descriptor_type)cfg.bDescriptorType,
            cfg.wTotalLength,
            cfg.bNumInterfaces,
            cfg.bConfigurationValue,
            cfg.iConfiguration,
            (libusb_config_desc_attributes)cfg.bmAttributes,
            cfg.MaxPower,
            interfaces,
            extraCfg
        );
    }

    private static TManaged[] ReadArray<TManaged>(
        nint basePtr,
        int count,
        Func<nint, TManaged> projector,
        int elementSize
    )
    {
        if (count <= 0 || basePtr == IntPtr.Zero)
            return Array.Empty<TManaged>();

        var arr = new TManaged[count];
        for (var i = 0; i < count; i++)
        {
            var elemPtr = IntPtr.Add(basePtr, i * elementSize);
            arr[i] = projector(elemPtr);
        }
        return arr;
    }

    private static byte[] ReadExtra(nint p, int length)
    {
        if (p == IntPtr.Zero || length <= 0)
            return Array.Empty<byte>();
        var bytes = new byte[length];
        Marshal.Copy(p, bytes, 0, length);
        return bytes;
    }
}
