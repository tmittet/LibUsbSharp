using System.Runtime.InteropServices;
using LibUsbNative.Descriptors;

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
            var config = LibusbConfigMarshaler.FromPointer(descriptor);
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
            var config = LibusbConfigMarshaler.FromPointer(descriptor);
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
}
