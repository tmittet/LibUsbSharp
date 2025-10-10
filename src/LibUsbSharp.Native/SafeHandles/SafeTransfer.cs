using System.Runtime.InteropServices;
using LibUsbSharp.Native.Enums;

namespace LibUsbSharp.Native.SafeHandles;

internal sealed class SafeTransfer : SafeHandle, ISafeTransfer
{
    private readonly SafeContext _context;

    public SafeTransfer(SafeContext context, nint ptr)
        : base(ptr, true)
    {
        _context = context;
        SetHandle(ptr);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
            return true;

        _context.Api.libusb_free_transfer(handle);
        return true;
    }

    public nint GetBufferPtr()
    {
        SafeHelper.ThrowIfClosed(this);
        return DangerousGetHandle();
    }

    public libusb_error Submit()
    {
        SafeHelper.ThrowIfClosed(this);
        return _context.Api.libusb_submit_transfer(handle);
    }

    public libusb_error Cancel()
    {
        SafeHelper.ThrowIfClosed(this);
        return _context.Api.libusb_cancel_transfer(handle);
    }
}
