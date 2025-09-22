using System.Runtime.InteropServices;
using LibUsbNative.Descriptors;
using LibUsbNative.Enums;

namespace LibUsbNative.SafeHandles;

public interface ISafeDevice
{
    ISafeDeviceHandle Open();

    IUsbDeviceDescriptor GetDeviceDescriptor();

    IUsbConfigDescriptor GetActiveConfigDescriptor();
    ISafeConfigDescriptorPtr GetActiveConfigDescriptorPtr();

    IUsbConfigDescriptor GetConfigDescriptor(byte config_index);
    ISafeConfigDescriptorPtr GetConfigDescriptorPtr(byte config_index);

    byte GetBusNumber();
    byte GetDeviceAddress();
    byte GetPortNumber();
}

internal sealed class SafeDevice : SafeHandle, ISafeDevice
{
    internal readonly SafeContext _context;

    public SafeDevice(SafeContext context, IntPtr dev)
        : base(dev, ownsHandle: true)
    {
        if (dev == IntPtr.Zero)
            throw new ArgumentNullException(nameof(dev));

        _context = context;
        _context.api.libusb_ref_device(dev);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid || IsClosed)
            return true;

        _context.api.libusb_unref_device(handle);
        _context.DangerousRelease();
        return true;
    }

    public IUsbDeviceDescriptor GetDeviceDescriptor()
    {
        SafeHelpers.ThrowIfClosed(this);

        var result = _context.api.libusb_get_device_descriptor(handle, out var d);
        LibUsbException.ThrowIfError(result);

        return new UsbDeviceDescriptor(
            d.bLength,
            (UsbDescriptorType)d.bDescriptorType,
            d.bcdUSB,
            (UsbClass)d.bDeviceClass,
            d.bDeviceSubClass,
            d.bDeviceProtocol,
            d.bMaxPacketSize0,
            d.idVendor,
            d.idProduct,
            d.bcdDevice,
            d.iManufacturer,
            d.iProduct,
            d.iSerialNumber,
            d.bNumConfigurations
        );
    }

    public ISafeConfigDescriptorPtr GetActiveConfigDescriptorPtr()
    {
        SafeHelpers.ThrowIfClosed(this);

        var result = _context.api.libusb_get_active_config_descriptor(handle, out var descriptor);
        if (result != LibUsbError.Success)
        {
            throw new LibUsbException(result, "Failed to get active configuration descriptor.");
        }

        bool success = false;
        DangerousAddRef(ref success);
        if (!success)
        {
            _context.api.libusb_free_config_descriptor(descriptor);
            LibUsbException.ThrowIfError(LibUsbError.Other, "Failed to ref SafeHandle");
        }

        return new SafeConfigDescriptorPtr(this, descriptor);
    }

    public IUsbConfigDescriptor GetActiveConfigDescriptor()
    {
        SafeHelpers.ThrowIfClosed(this);

        var result = _context.api.libusb_get_active_config_descriptor(handle, out var descriptor);
        if (result != LibUsbError.Success)
        {
            throw new LibUsbException(result, "Failed to get active configuration descriptor.");
        }
        try
        {
            var config = FromPointer(descriptor);
            return config;
        }
        finally
        {
            _context.api.libusb_free_config_descriptor(descriptor);
        }
    }

    public ISafeConfigDescriptorPtr GetConfigDescriptorPtr(byte config_index)
    {
        SafeHelpers.ThrowIfClosed(this);

        var result = _context.api.libusb_get_config_descriptor(handle, config_index, out var descriptor);
        if (result != LibUsbError.Success)
        {
            throw new LibUsbException(result, "Failed to get configuration descriptor.");
        }

        bool success = false;
        DangerousAddRef(ref success);
        if (!success)
        {
            _context.api.libusb_free_config_descriptor(descriptor);
            LibUsbException.ThrowIfError(LibUsbError.Other, "Failed to ref SafeHandle");
        }

        return new SafeConfigDescriptorPtr(this, descriptor);
    }

    public IUsbConfigDescriptor GetConfigDescriptor(byte config_index)
    {
        SafeHelpers.ThrowIfClosed(this);

        var result = _context.api.libusb_get_config_descriptor(handle, config_index, out var descriptor);
        if (result != LibUsbError.Success)
        {
            throw new LibUsbException(result, "Failed to get configuration descriptor.");
        }
        try
        {
            var config = FromPointer(descriptor);
            return config;
        }
        finally
        {
            _context.api.libusb_free_config_descriptor(descriptor);
        }
    }

    public byte GetBusNumber()
    {
        SafeHelpers.ThrowIfClosed(this);
        return _context.api.libusb_get_bus_number(handle);
    }

    public byte GetDeviceAddress()
    {
        SafeHelpers.ThrowIfClosed(this);
        return _context.api.libusb_get_device_address(handle);
    }

    public byte GetPortNumber()
    {
        SafeHelpers.ThrowIfClosed(this);
        return _context.api.libusb_get_port_number(handle);
    }

    public ISafeDeviceHandle Open()
    {
        SafeHelpers.ThrowIfClosed(this);

        var result = _context.api.libusb_open(handle, out var ptr);
        if (result != LibUsbError.Success)
            throw new LibUsbException(result, "Failed to open USB device.");

        bool success = false;
        _context.DangerousAddRef(ref success);
        if (!success)
        {
            _context.api.libusb_close(ptr);
            LibUsbException.ThrowIfError(LibUsbError.Other, "Failed to ref SafeHandle");
        }

        return new SafeDeviceHandle(_context, ptr, new SafeDevice(_context, handle));
    }

    private static UsbConfigDescriptor FromPointer(IntPtr pConfigDescriptor)
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

                                return new UsbEndpointDescriptor(
                                    ep.bLength,
                                    (UsbDescriptorType)ep.bDescriptorType,
                                    new UsbEndpointAddress(ep.bEndpointAddress),
                                    new UsbEndpointAttributes(ep.bmAttributes),
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

                        return new UsbInterfaceDescriptor(
                            id.bLength,
                            (UsbDescriptorType)id.bDescriptorType,
                            id.bInterfaceNumber,
                            id.bAlternateSetting,
                            id.bNumEndpoints,
                            (UsbClass)id.bInterfaceClass,
                            id.bInterfaceSubClass,
                            id.bInterfaceProtocol,
                            id.iInterface,
                            endpoints,
                            extraIf
                        );
                    },
                    Marshal.SizeOf<native_libusb_interface_descriptor>()
                );

                return new UsbInterface(alt);
            },
            Marshal.SizeOf<native_libusb_interface>()
        );

        var extraCfg = ReadExtra(cfg.extra, cfg.extra_length);

        return new UsbConfigDescriptor(
            cfg.bLength,
            (UsbDescriptorType)cfg.bDescriptorType,
            cfg.wTotalLength,
            cfg.bNumInterfaces,
            cfg.bConfigurationValue,
            cfg.iConfiguration,
            (UsbConfigAttributes)cfg.bmAttributes,
            cfg.MaxPower,
            interfaces,
            extraCfg
        );
    }

    private static TManaged[] ReadArray<TManaged>(
        IntPtr basePtr,
        int count,
        Func<IntPtr, TManaged> projector,
        int elementSize
    )
    {
        if (count <= 0 || basePtr == IntPtr.Zero)
            return Array.Empty<TManaged>();

        var arr = new TManaged[count];
        for (int i = 0; i < count; i++)
        {
            var elemPtr = IntPtr.Add(basePtr, i * elementSize);
            arr[i] = projector(elemPtr);
        }
        return arr;
    }

    private static byte[] ReadExtra(IntPtr p, int length)
    {
        if (p == IntPtr.Zero || length <= 0)
            return Array.Empty<byte>();
        var bytes = new byte[length];
        Marshal.Copy(p, bytes, 0, length);
        return bytes;
    }
}
