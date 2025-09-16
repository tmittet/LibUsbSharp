using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibUsbNative.SafeHandles;

public interface ISafeDeviceList : IDisposable
{
    IEnumerable<ISafeDevice> Devices { get; }
    bool IsClosed { get; }
}

internal sealed class SafeDeviceList : SafeHandle, ISafeDeviceList
{
    private readonly uint _count;
    private readonly Lazy<SafeDevice[]> lazyDevices;
    private readonly SafeContext _context;

    public SafeDeviceList(SafeContext context, IntPtr listPtr, uint count)
        : base(listPtr, ownsHandle: true)
    {
        if (listPtr == IntPtr.Zero)
            throw new ArgumentNullException(nameof(listPtr));

        _context = context;
        _count = count;
        lazyDevices = new Lazy<SafeDevice[]>(() =>
        {
            var devices = new SafeDevice[_count];
            var ptrSize = IntPtr.Size;
            for (var i = 0; i < _count; i++)
            {
                var devPtr = Marshal.ReadIntPtr(handle, i * ptrSize);

                bool success = false;
                context.DangerousAddRef(ref success);
                if (!success)
                {
                    LibUsbException.ThrowIfError(LibUsbError.Other, "Failed to ref SafeHandle");
                }

                devices[i] = new SafeDevice(_context, devPtr);
            }
            return devices;
        });
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    public uint Count
    {
        get
        {
            SafeHelpers.ThrowIfClosed(this);
            return _count;
        }
    }

    public IEnumerable<ISafeDevice> Devices
    {
        get
        {
            SafeHelpers.ThrowIfClosed(this);
            return lazyDevices.Value;
        }
    }

    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
            return true;

        if (lazyDevices.IsValueCreated)
        {
            foreach (var device in lazyDevices.Value)
            {
                if (!device.IsClosed)
                {
                    device.Dispose();
                }
            }
        }

        _context.api.libusb_free_device_list(handle, unrefDevices: 1);
        _context.DangerousRelease();
        return true;
    }
}
