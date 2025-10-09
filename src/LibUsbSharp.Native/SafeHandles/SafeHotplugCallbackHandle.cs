using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace LibUsbSharp.Native.SafeHandles;

internal sealed class SafeHotplugCallbackHandle : SafeHandleZeroOrMinusOneIsInvalid, ISafeCallbackHandle
{
    private readonly SafeContext _context;
    private readonly GCHandle _gcHandle;

    public SafeHotplugCallbackHandle(SafeContext context, GCHandle gcHandle, nint handle)
        : base(ownsHandle: true)
    {
        if (!gcHandle.IsAllocated)
        {
            throw new ArgumentException("GCHandle not allocated.", nameof(gcHandle));
        }
        _context = context;
        _gcHandle = gcHandle;
        this.handle = handle;
    }

    protected override bool ReleaseHandle()
    {
        _context.Api.libusb_hotplug_deregister_callback(_context.DangerousGetHandle(), handle);
        _gcHandle.Free();
        _context.DangerousRelease();
        handle = IntPtr.Zero;
        return true;
    }
}
