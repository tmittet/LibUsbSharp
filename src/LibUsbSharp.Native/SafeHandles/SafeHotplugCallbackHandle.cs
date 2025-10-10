using System.Runtime.InteropServices;

namespace LibUsbSharp.Native.SafeHandles;

internal sealed class SafeHotplugCallbackHandle : SafeHandle, ISafeCallbackHandle
{
    private readonly SafeContext _context;
    private readonly GCHandle _gcHandle;

    public override bool IsInvalid => handle == IntPtr.Zero;

    public SafeHotplugCallbackHandle(SafeContext context, GCHandle gcHandle, nint callbackHandle)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        if (!gcHandle.IsAllocated)
        {
            throw new ArgumentException("GCHandle not allocated.", nameof(gcHandle));
        }
        if (callbackHandle == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(callbackHandle));
        }

        _context = context;
        _gcHandle = gcHandle;
        handle = callbackHandle;
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
