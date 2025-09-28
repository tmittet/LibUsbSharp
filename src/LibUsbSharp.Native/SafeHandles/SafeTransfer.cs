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

        _context.api.libusb_free_transfer(handle);
        return true;
    }

    public nint GetBufferPtr()
    {
        SafeHelpers.ThrowIfClosed(this);
        return DangerousGetHandle();
    }

    public libusb_error Submit()
    {
        SafeHelpers.ThrowIfClosed(this);
        return _context.api.libusb_submit_transfer(handle);
    }

    public libusb_error Cancel()
    {
        SafeHelpers.ThrowIfClosed(this);
        return _context.api.libusb_cancel_transfer(handle);
    }
}
