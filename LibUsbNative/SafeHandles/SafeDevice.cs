using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibUsbNative.SafeHandles;

public interface ISafeDevice
{
    ISafeDeviceHandle Open();
    IUsbConfigDescriptor GetActiveConfigDescriptor();
    ISafeConfigDescriptorPtr GetActiveConfigDescriptorPtr();
    IUsbDeviceDescriptor GetDeviceDescriptor();
    byte GetBusNumber();
    byte GetDeviceAddress();
    byte GetPortNumber();
}

internal sealed class SafeDevice : SafeHandle, ISafeDevice
{
    private readonly SafeContext _context;

    public SafeDevice(SafeContext context, IntPtr dev)
        : base(dev, ownsHandle: true)
    {
        if (dev == IntPtr.Zero)
            throw new ArgumentNullException(nameof(dev));

        _context = context;
        LibUsb.Api.libusb_ref_device(dev);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid || IsClosed)
            return true;

        LibUsb.Api.libusb_unref_device(handle);
        _context.DangerousRelease();
        return true;
    }

    public IUsbDeviceDescriptor GetDeviceDescriptor()
    {
        if (IsClosed || IsInvalid)
        {
            throw new ObjectDisposedException(nameof(SafeDevice));
        }

        var result = LibUsb.Api.libusb_get_device_descriptor(handle, out var d);
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
        if (IsClosed || IsInvalid)
        {
            throw new ObjectDisposedException(nameof(SafeDevice));
        }

        var result = LibUsb.Api.libusb_get_active_config_descriptor(handle, out var descriptor);
        if (result != LibUsbError.Success)
        {
            throw new LibUsbException(result, "Failed to get active configuration descriptor.");
        }

        bool success = false;
        DangerousAddRef(ref success);
        if (!success)
        {
            LibUsb.Api.libusb_free_config_descriptor(descriptor);
            LibUsbException.ThrowIfError(LibUsbError.Other, "Failed to ref SafeHandle");
        }

        return new SafeConfigDescriptorPtr(this, descriptor);
    }

    public IUsbConfigDescriptor GetActiveConfigDescriptor()
    {
        if (IsClosed || IsInvalid)
        {
            throw new ObjectDisposedException(nameof(SafeDevice));
        }

        var result = LibUsb.Api.libusb_get_active_config_descriptor(handle, out var descriptor);
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
            LibUsb.Api.libusb_free_config_descriptor(descriptor);
        }
    }

    public byte GetBusNumber()
    {
        if (IsClosed || IsInvalid)
        {
            throw new ObjectDisposedException(nameof(SafeDevice));
        }

        return LibUsb.Api.libusb_get_bus_number(handle);
    }

    public byte GetDeviceAddress()
    {
        if (IsClosed || IsInvalid)
        {
            throw new ObjectDisposedException(nameof(SafeDevice));
        }

        return LibUsb.Api.libusb_get_device_address(handle);
    }

    public byte GetPortNumber()
    {
        if (IsClosed || IsInvalid)
        {
            throw new ObjectDisposedException(nameof(SafeDevice));
        }

        return LibUsb.Api.libusb_get_port_number(handle);
    }

    public ISafeDeviceHandle Open()
    {
        if (IsClosed || IsInvalid)
        {
            throw new ObjectDisposedException(nameof(SafeDevice));
        }

        var result = LibUsb.Api.libusb_open(handle, out var ptr);
        if (result != LibUsbError.Success)
            throw new LibUsbException(result, "Failed to open USB device.");

        bool success = false;
        _context.DangerousAddRef(ref success);
        if (!success)
        {
            LibUsb.Api.libusb_close(ptr);
            LibUsbException.ThrowIfError(LibUsbError.Other, "Failed to ref SafeHandle");
        }

        return new SafeDeviceHandle(_context, ptr, new SafeDevice(_context, handle));
    }
}
