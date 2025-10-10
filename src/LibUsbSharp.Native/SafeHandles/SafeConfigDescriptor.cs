using System.Runtime.InteropServices;

namespace LibUsbSharp.Native.SafeHandles;

internal sealed class SafeConfigDescriptor : SafeHandle, ISafeConfigDescriptor
{
    private readonly SafeDevice _device;

    public override bool IsInvalid => handle == IntPtr.Zero;

    public SafeConfigDescriptor(SafeDevice device, nint configHandle)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        if (configHandle == IntPtr.Zero)
            throw new ArgumentNullException(nameof(configHandle));

        _device = device;
        handle = configHandle;
    }

    protected override bool ReleaseHandle()
    {
        if (IsInvalid || IsClosed)
            return true;

        _device.Api.libusb_free_config_descriptor(handle);
        _device.DangerousRelease();
        return true;
    }

    public nint GetUnmanagedPointer()
    {
        SafeHelper.ThrowIfClosed(this);
        return DangerousGetHandle();
    }
}
