using System;
using System.Runtime.InteropServices;

namespace LibUsbNative.SafeHandles;

public interface ISafeDeviceHandle : IDisposable
{
    ISafeDevice Device { get; }
    bool IsClosed { get; }

    IntPtr DangerousGetHandle();
    string GetStringDescriptorAscii(byte index);
    ISafeDeviceInterface ClaimInterface(int interfaceNumber);
    LibUsbError ResetDevice();
}

internal sealed class SafeDeviceHandle : SafeHandle, ISafeDeviceHandle
{
    private readonly SafeDevice _device;
    public ISafeDevice Device => _device;
    private readonly SafeContext _context;

    public SafeDeviceHandle(SafeContext context, IntPtr deviceHandle, SafeDevice device)
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

        LibUsb.Api.libusb_close(handle);
        _device.Dispose();
        _context.DangerousRelease();
        return true;
    }

    public string GetStringDescriptorAscii(byte index)
    {
        var buf = new byte[256];
        var result = LibUsb.Api.libusb_get_string_descriptor_ascii(handle, index, buf, buf.Length);
        LibUsbException.ThrowIfError(result, "Failed to get string descriptor at index {index}");
        return System.Text.Encoding.ASCII.GetString(buf, 0, (int)result);
    }

    public ISafeDeviceInterface ClaimInterface(int interfaceNumber)
    {
        var result = LibUsb.Api.libusb_claim_interface(handle, interfaceNumber);
        LibUsbException.ThrowIfError(result, $"Failed to claim interface {interfaceNumber}");
        return new SafeDeviceInterface(this, interfaceNumber);
    }

    public LibUsbError ResetDevice()
    {
        return LibUsb.Api.libusb_reset_device(handle);
    }

    /*
        public static ushort GetFirstLanguageId(this SafeDeviceHandle handle)
        {
            if (handle is null) throw new ArgumentNullException(nameof(handle));
            var buf = new byte[255];
            int rc = Libusb.Api.libusb_get_string_descriptor(handle.DangerousGetHandle(), 0, 0, buf, buf.Length);
            if (rc < 4) return 0;
            return (ushort)(buf[2] | (buf[3] << 8));
        }

        public static string? GetStringUtf16(this SafeDeviceHandle handle, byte index, ushort? langId = null)
        {
            if (handle is null) throw new ArgumentNullException(nameof(handle));
            if (index == 0) return null;
            var lang = langId ?? handle.GetFirstLanguageId();
            if (lang == 0) return null;
            var buf = new byte[256];
            int rc = Libusb.Api.libusb_get_string_descriptor(handle.DangerousGetHandle(), index, lang, buf, buf.Length);
            if (rc < 2) return null;
            int byteLen = Math.Min(buf[0], rc);
            int payload = byteLen - 2;
            if (payload <= 0) return string.Empty;
            return System.Text.Encoding.Unicode.GetString(buf, 2, payload);
        }
     *    public static void SetConfiguration(this SafeDeviceHandle handle, int configuration) =>
            LibUsbException.ThrowIfError(Libusb.Api.libusb_set_configuration(handle.DangerousGetHandle(), configuration), nameof(Libusb.Api.libusb_set_configuration));

        public static int GetConfiguration(this SafeDeviceHandle handle)
        {
            int cfg;
            LibUsbException.ThrowIfError(Libusb.Api.libusb_get_configuration(handle.DangerousGetHandle(), out cfg), nameof(Libusb.Api.libusb_get_configuration));
            return cfg;
        }

        public static void SetAltSetting(this SafeDeviceHandle handle, int iface, int alt) =>
            LibUsbException.ThrowIfError(Libusb.Api.libusb_set_interface_alt_setting(handle.DangerousGetHandle(), iface, alt), nameof(Libusb.Api.libusb_set_interface_alt_setting));

        // -------- Kernel driver --------
        public static bool IsKernelDriverActive(this SafeDeviceHandle handle, int iface) =>
            Libusb.Api.libusb_kernel_driver_active(handle.DangerousGetHandle(), iface) == 1;

        public static void DetachKernelDriver(this SafeDeviceHandle handle, int iface) =>
            LibUsbException.ThrowIfError(Libusb.Api.libusb_detach_kernel_driver(handle.DangerousGetHandle(), iface), nameof(Libusb.Api.libusb_detach_kernel_driver));

        public static void AttachKernelDriver(this SafeDeviceHandle handle, int iface) =>
            LibUsbException.ThrowIfError(Libusb.Api.libusb_attach_kernel_driver(handle.DangerousGetHandle(), iface), nameof(Libusb.Api.libusb_attach_kernel_driver));

        public static void SetAutoDetachKernelDriver(this SafeDeviceHandle handle, bool enable) =>
            LibUsbException.ThrowIfError(Libusb.Api.libusb_set_auto_detach_kernel_driver(handle.DangerousGetHandle(), enable ? 1 : 0), nameof(Libusb.Api.libusb_set_auto_detach_kernel_driver));

        // -------- Sync I/O --------
        public static int ControlTransfer(this SafeDeviceHandle handle, byte bmRequestType, byte bRequest, ushort wValue, ushort wIndex, Span<byte> buffer, uint timeoutMs = 5000, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var arr = buffer.ToArray();
            int rc = Libusb.Api.libusb_control_transfer(handle.DangerousGetHandle(), bmRequestType, bRequest, wValue, wIndex, arr, (ushort)arr.Length, timeoutMs);
            LibUsbException.ThrowIfError(rc, nameof(Libusb.Api.libusb_control_transfer));
            arr.AsSpan(0, Math.Min(rc, arr.Length)).CopyTo(buffer);
            return rc;
        }

        public static int BulkIn(this SafeDeviceHandle handle, byte endpoint, Span<byte> buffer, uint timeoutMs = 5000, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var arr = buffer.ToArray();
            int rc = Libusb.Api.libusb_bulk_transfer(handle.DangerousGetHandle(), endpoint, arr, arr.Length, out int got, timeoutMs);
            LibUsbException.ThrowIfError(rc, nameof(Libusb.Api.libusb_bulk_transfer), $"ep=0x{endpoint:X2}");
            arr.AsSpan(0, got).CopyTo(buffer);
            return got;
        }

        public static int BulkOut(this SafeDeviceHandle handle, byte endpoint, ReadOnlySpan<byte> data, uint timeoutMs = 5000, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var arr = data.ToArray();
            int rc = Libusb.Api.libusb_bulk_transfer(handle.DangerousGetHandle(), endpoint, arr, arr.Length, out int sent, timeoutMs);
            LibUsbException.ThrowIfError(rc, nameof(Libusb.Api.libusb_bulk_transfer), $"ep=0x{endpoint:X2}");
            return sent;
        }

        public static int InterruptTransfer(this SafeDeviceHandle handle, byte endpoint, Span<byte> buffer, uint timeoutMs = 5000, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var arr = buffer.ToArray();
            int rc = Libusb.Api.libusb_interrupt_transfer(handle.DangerousGetHandle(), endpoint, arr, arr.Length, out int got, timeoutMs);
            LibUsbException.ThrowIfError(rc, nameof(Libusb.Api.libusb_interrupt_transfer), $"ep=0x{endpoint:X2}");
            arr.AsSpan(0, got).CopyTo(buffer);
            return got;
        }

        // -------- Halt/Reset --------
        public static void ClearHalt(this SafeDeviceHandle handle, byte endpoint) =>
            LibUsbException.ThrowIfError(Libusb.Api.libusb_clear_halt(handle.DangerousGetHandle(), endpoint), nameof(Libusb.Api.libusb_clear_halt));

        public static void ResetDevice(this SafeDeviceHandle handle) =>
            LibUsbException.ThrowIfError(Libusb.Api.libusb_reset_device(handle.DangerousGetHandle()), nameof(Libusb.Api.libusb_reset_device));
    }
    }
    */
}
