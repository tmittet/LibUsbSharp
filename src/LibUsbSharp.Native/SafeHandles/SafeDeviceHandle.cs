using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using LibUsbSharp.Native.Enums;

namespace LibUsbSharp.Native.SafeHandles;

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

        _context.Api.libusb_close(handle);
        _device.Dispose();
        _context.DangerousRelease();
        return true;
    }

    /// <inheritdoc />
    public string GetStringDescriptorAscii(byte index)
    {
        return TryGetStringDescriptorAscii(index, out var value, out var error)
            ? value
            : throw LibUsbException.FromApiError(error.Value, nameof(_context.Api.libusb_get_string_descriptor_ascii));
    }

    /// <inheritdoc />
    public bool TryGetStringDescriptorAscii(
        byte index,
        [NotNullWhen(true)] out string? descriptorValue,
        [NotNullWhen(false)] out libusb_error? usbError
    )
    {
        SafeHelpers.ThrowIfClosed(this);

        var buffer = new byte[256];
        var result = _context.Api.libusb_get_string_descriptor_ascii(handle, index, buffer, buffer.Length);

        if (result >= 0)
        {
            descriptorValue = Encoding.ASCII.GetString(buffer, 0, (int)result);
            usbError = null;
            return true;
        }

        descriptorValue = null;
        usbError = result;
        return false;
    }

    public ISafeDeviceInterface ClaimInterface(byte interfaceNumber)
    {
        SafeHelpers.ThrowIfClosed(this);

        var result = _context.Api.libusb_claim_interface(handle, interfaceNumber);
        LibUsbException.ThrowIfApiError(
            result,
            nameof(_context.Api.libusb_claim_interface),
            $"Interface {interfaceNumber}."
        );
        return new SafeDeviceInterface(this, interfaceNumber);
    }

    /// <inheritdoc />
    public void ResetDevice()
    {
        SafeHelpers.ThrowIfClosed(this);
        var result = _context.Api.libusb_reset_device(handle);
        LibUsbException.ThrowIfApiError(result, nameof(_context.Api.libusb_reset_device));
    }

    /// <inheritdoc />
    public ISafeTransfer AllocateTransfer(int isoPackets = 0)
    {
        if (isoPackets < 0)
            throw new ArgumentOutOfRangeException(nameof(isoPackets), "Must be greater than or equal to zero.");

        var ptr = _context.Api.libusb_alloc_transfer(isoPackets);
        return ptr == IntPtr.Zero
            ? throw new LibUsbException(
                libusb_error.LIBUSB_ERROR_NO_MEM,
                $"LibUsbApi '{nameof(_context.Api.libusb_alloc_transfer)}' failed."
            )
            : (ISafeTransfer)new SafeTransfer(_context, ptr);
    }
}
