using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibUsbNative.SafeHandles;

public interface ISafeTransfer : IDisposable
{
    LibUsbError Submit();
    LibUsbError Cancel();
    IntPtr GetBufferPtr();
    static ISafeTransfer Allocate(int isoPackets = 0)
    {
        {
            var ptr = LibUsbNative.Api.libusb_alloc_transfer(isoPackets);
            if (ptr == IntPtr.Zero)
                throw new LibUsbException(LibUsbError.NoMem, "Failed to allocate libusb transfer buffer.");

            return new SafeTransfer(ptr);
        }
    }
}

internal sealed class SafeTransfer : SafeHandle, ISafeTransfer
{
    public SafeTransfer(IntPtr ptr)
        : base(ptr, true)
    {
        SetHandle(ptr);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
            return true;

        LibUsbNative.Api.libusb_free_transfer(handle);
        return true;
    }

    public IntPtr GetBufferPtr()
    {
        SafeHelpers.ThrowIfClosed(this);
        return DangerousGetHandle();
    }

    public LibUsbError Submit()
    {
        SafeHelpers.ThrowIfClosed(this);
        return LibUsbNative.Api.libusb_submit_transfer(handle);
    }

    public LibUsbError Cancel()
    {
        SafeHelpers.ThrowIfClosed(this);
        return LibUsbNative.Api.libusb_cancel_transfer(handle);
    }
}
