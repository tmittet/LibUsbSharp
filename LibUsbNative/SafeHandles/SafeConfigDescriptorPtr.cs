using System;
using System.Runtime.InteropServices;

namespace LibUsbNative.SafeHandles;

public interface ISafeConfigDescriptorPtr : IDisposable
{
    IntPtr GetUnmanagedPointer();
}

internal sealed class SafeConfigDescriptorPtr : SafeHandle, ISafeConfigDescriptorPtr
{
    private readonly SafeDevice _device;

    public SafeConfigDescriptorPtr(SafeDevice device, IntPtr configPtr)
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

        LibUsbNative.Api.libusb_free_config_descriptor(handle);
        _device.DangerousRelease();
        return true;
    }

    public IntPtr GetUnmanagedPointer()
    {
        SafeHelpers.ThrowIfClosed(this);
        return DangerousGetHandle();
    }
}
