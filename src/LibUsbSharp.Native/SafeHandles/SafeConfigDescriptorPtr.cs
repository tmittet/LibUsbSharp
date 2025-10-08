using System.Runtime.InteropServices;

namespace LibUsbSharp.Native.SafeHandles;

internal sealed class SafeConfigDescriptorPtr : SafeHandle, ISafeConfigDescriptorPtr
{
    private readonly SafeDevice _device;

    public SafeConfigDescriptorPtr(SafeDevice device, nint configPtr)
        : base(configPtr, ownsHandle: true)
    {
        if (configPtr == IntPtr.Zero)
            throw new ArgumentNullException(nameof(configPtr));

        _device = device;
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid || IsClosed)
            return true;

        _device._context.Api.libusb_free_config_descriptor(handle);
        _device.DangerousRelease();
        return true;
    }

    public nint GetUnmanagedPointer()
    {
        SafeHelpers.ThrowIfClosed(this);
        return DangerousGetHandle();
    }
}
