using System.Runtime.InteropServices;
using LibUsbNative.Enums;

namespace LibUsbNative.SafeHandles;

internal sealed class SafeDeviceHandle : SafeHandle, ISafeDeviceHandle
{
    internal readonly SafeContext _context;
    private readonly SafeDevice _device;

    /// <inheritdoc />
    public ISafeDevice Device
    {
        get
        {
            SafeHelpers.ThrowIfClosed(this);
            return _device;
        }
    }

    public SafeDeviceHandle(SafeContext context, nint deviceHandle, SafeDevice device)
        : base(deviceHandle, ownsHandle: true)
    {
        if (deviceHandle == IntPtr.Zero)
            throw new ArgumentNullException(nameof(deviceHandle));

        _context = context;
        _device = device;
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
            return true;

        _context.api.libusb_close(handle);
        _device.Dispose();
        _context.DangerousRelease();
        return true;
    }

    /// <inheritdoc />
    public string GetStringDescriptorAscii(byte index)
    {
        SafeHelpers.ThrowIfClosed(this);

        var buf = new byte[256];
        var result = _context.api.libusb_get_string_descriptor_ascii(handle, index, buf, buf.Length);
        LibUsbException.ThrowIfApiError(
            result,
            nameof(_context.api.libusb_get_string_descriptor_ascii),
            $"Index {index}."
        );
        return System.Text.Encoding.ASCII.GetString(buf, 0, (int)result);
    }

    public ISafeDeviceInterface ClaimInterface(int interfaceNumber)
    {
        SafeHelpers.ThrowIfClosed(this);

        var result = _context.api.libusb_claim_interface(handle, interfaceNumber);
        LibUsbException.ThrowIfApiError(
            result,
            nameof(_context.api.libusb_claim_interface),
            $"Interface {interfaceNumber}."
        );
        return new SafeDeviceInterface(this, interfaceNumber);
    }

    /// <inheritdoc />
    public void ResetDevice()
    {
        SafeHelpers.ThrowIfClosed(this);
        var result = _context.api.libusb_reset_device(handle);
        LibUsbException.ThrowIfApiError(result, nameof(_context.api.libusb_reset_device));
    }

    /// <inheritdoc />
    public ISafeTransfer AllocateTransfer(int isoPackets = 0)
    {
        if (isoPackets < 0)
            throw new ArgumentOutOfRangeException(nameof(isoPackets), "Must be greater than or equal to zero.");

        var ptr = _context.api.libusb_alloc_transfer(isoPackets);
        return ptr == IntPtr.Zero
            ? throw new LibUsbException(
                libusb_error.LIBUSB_ERROR_NO_MEM,
                $"LibUsbApi '{nameof(_context.api.libusb_alloc_transfer)}' failed."
            )
            : (ISafeTransfer)new SafeTransfer(_context, ptr);
    }
}
