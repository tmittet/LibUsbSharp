using System;
using System.Threading;
using LibUsbNative.SafeHandles;

namespace LibUsbNative.Extensions;

public static class SafeDeviceHandleExtensions
{
    /*
    // -------- String descriptors --------
    public static string? GetStringAscii(this SafeDeviceHandle handle, byte index)
    {
        if (handle is null)
            throw new ArgumentNullException(nameof(handle));
        if (index == 0)
            return null;
        var buf = new byte[256];
        var rc = LibUsb.Api.libusb_get_string_descriptor_ascii(handle.DangerousGetHandle(), index, buf, buf.Length);
        if (rc <= 0)
            return null;
        return System.Text.Encoding.ASCII.GetString(buf, 0, (int)rc);
    }

    public static ushort GetFirstLanguageId(this SafeDeviceHandle handle)
    {
        if (handle is null)
            throw new ArgumentNullException(nameof(handle));
        var buf = new byte[255];
        var rc = LibUsb.Api.libusb_get_string_descriptor(handle.DangerousGetHandle(), 0, 0, buf, buf.Length);
        if ((int)rc < 4)
            return 0;
        return (ushort)(buf[2] | buf[3] << 8);
    }

    public static string? GetStringUtf16(this SafeDeviceHandle handle, byte index, ushort? langId = null)
    {
        if (handle is null)
            throw new ArgumentNullException(nameof(handle));
        if (index == 0)
            return null;
        var lang = langId ?? handle.GetFirstLanguageId();
        if (lang == 0)
            return null;
        var buf = new byte[256];
        var rc = (int)
            LibUsb.Api.libusb_get_string_descriptor(handle.DangerousGetHandle(), index, lang, buf, buf.Length);
        if ((int)rc < 2)
            return null;
        var byteLen = Math.Min(buf[0], rc);
        var payload = byteLen - 2;
        if (payload <= 0)
            return string.Empty;
        return System.Text.Encoding.Unicode.GetString(buf, 2, payload);
    }

    // -------- Config / Interface mgmt --------
    public static void SetConfiguration(this SafeDeviceHandle handle, int configuration) =>
        LibUsbException.ThrowIfError(
            LibUsb.Api.libusb_set_configuration(handle.DangerousGetHandle(), configuration),
            nameof(LibUsb.Api.libusb_set_configuration)
        );

    public static int GetConfiguration(this SafeDeviceHandle handle)
    {
        int cfg;
        LibUsbException.ThrowIfError(
            LibUsb.Api.libusb_get_configuration(handle.DangerousGetHandle(), out cfg),
            nameof(LibUsb.Api.libusb_get_configuration)
        );
        return cfg;
    }

    public static void ClaimInterface(this SafeDeviceHandle handle, int iface) =>
        LibUsbException.ThrowIfError(
            LibUsb.Api.libusb_claim_interface(handle.DangerousGetHandle(), iface),
            nameof(LibUsb.Api.libusb_claim_interface)
        );

    public static void ReleaseInterface(this SafeDeviceHandle handle, int iface) =>
        LibUsbException.ThrowIfError(
            LibUsb.Api.libusb_release_interface(handle.DangerousGetHandle(), iface),
            nameof(LibUsb.Api.libusb_release_interface)
        );

    public static void SetAltSetting(this SafeDeviceHandle handle, int iface, int alt) =>
        LibUsbException.ThrowIfError(
            LibUsb.Api.libusb_set_interface_alt_setting(handle.DangerousGetHandle(), iface, alt),
            nameof(LibUsb.Api.libusb_set_interface_alt_setting)
        );
    */
    /*
    // -------- Kernel driver --------
    public static bool IsKernelDriverActive(this SafeDeviceHandle handle, int iface) =>
        LibUsb.Api.libusb_kernel_driver_active(handle.DangerousGetHandle(), iface) == 1;

    public static void DetachKernelDriver(this SafeDeviceHandle handle, int iface) =>
        LibUsbException.ThrowIfError(
            LibUsb.Api.libusb_detach_kernel_driver(handle.DangerousGetHandle(), iface),
            nameof(LibUsb.Api.libusb_detach_kernel_driver)
        );

    public static void AttachKernelDriver(this SafeDeviceHandle handle, int iface) =>
        LibUsbException.ThrowIfError(
            LibUsb.Api.libusb_attach_kernel_driver(handle.DangerousGetHandle(), iface),
            nameof(LibUsb.Api.libusb_attach_kernel_driver)
        );

    public static void SetAutoDetachKernelDriver(this SafeDeviceHandle handle, bool enable) =>
        LibUsbException.ThrowIfError(
            LibUsb.Api.libusb_set_auto_detach_kernel_driver(handle.DangerousGetHandle(), enable ? 1 : 0),
            nameof(LibUsb.Api.libusb_set_auto_detach_kernel_driver)
        );
    */
    /*
    
        // -------- Sync I/O --------
        public static int ControlTransfer(
            this SafeDeviceHandle handle,
            byte bmRequestType,
            byte bRequest,
            ushort wValue,
            ushort wIndex,
            Span<byte> buffer,
            uint timeoutMs = 5000,
            CancellationToken ct = default
        )
        {
            ct.ThrowIfCancellationRequested();
            var arr = buffer.ToArray();
            var rc = LibUsb.Api.libusb_control_transfer(
                handle.DangerousGetHandle(),
                bmRequestType,
                bRequest,
                wValue,
                wIndex,
                arr,
                (ushort)arr.Length,
                timeoutMs
            );
            LibUsbException.ThrowIfError(rc, nameof(LibUsb.Api.libusb_control_transfer));
            arr.AsSpan(0, Math.Min((int)rc, arr.Length)).CopyTo(buffer);
            return (int)rc;
        }
    
        public static int BulkIn(
            this SafeDeviceHandle handle,
            byte endpoint,
            Span<byte> buffer,
            uint timeoutMs = 5000,
            CancellationToken ct = default
        )
        {
            ct.ThrowIfCancellationRequested();
            var arr = buffer.ToArray();
            var rc = LibUsb.Api.libusb_bulk_transfer(
                handle.DangerousGetHandle(),
                endpoint,
                arr,
                arr.Length,
                out var got,
                timeoutMs
            );
            LibUsbException.ThrowIfError(rc, nameof(LibUsb.Api.libusb_bulk_transfer), $"ep=0x{endpoint:X2}");
            arr.AsSpan(0, got).CopyTo(buffer);
            return got;
        }
    
        public static int BulkOut(
            this SafeDeviceHandle handle,
            byte endpoint,
            ReadOnlySpan<byte> data,
            uint timeoutMs = 5000,
            CancellationToken ct = default
        )
        {
            ct.ThrowIfCancellationRequested();
            var arr = data.ToArray();
            var rc = LibUsb.Api.libusb_bulk_transfer(
                handle.DangerousGetHandle(),
                endpoint,
                arr,
                arr.Length,
                out var sent,
                timeoutMs
            );
            LibUsbException.ThrowIfError(rc, nameof(LibUsb.Api.libusb_bulk_transfer), $"ep=0x{endpoint:X2}");
            return sent;
        }
    
        public static int InterruptTransfer(
            this SafeDeviceHandle handle,
            byte endpoint,
            Span<byte> buffer,
            uint timeoutMs = 5000,
            CancellationToken ct = default
        )
        {
            ct.ThrowIfCancellationRequested();
            var arr = buffer.ToArray();
            var rc = LibUsb.Api.libusb_interrupt_transfer(
                handle.DangerousGetHandle(),
                endpoint,
                arr,
                arr.Length,
                out var got,
                timeoutMs
            );
            LibUsbException.ThrowIfError(rc, nameof(LibUsb.Api.libusb_interrupt_transfer), $"ep=0x{endpoint:X2}");
            arr.AsSpan(0, got).CopyTo(buffer);
            return got;
        }
    
        // -------- Halt/Reset --------
        public static void ClearHalt(this SafeDeviceHandle handle, byte endpoint) =>
            LibUsbException.ThrowIfError(
                LibUsb.Api.libusb_clear_halt(handle.DangerousGetHandle(), endpoint),
                nameof(LibUsb.Api.libusb_clear_halt)
            );
    
        public static void ResetDevice(this SafeDeviceHandle handle) =>
            LibUsbException.ThrowIfError(
                LibUsb.Api.libusb_reset_device(handle.DangerousGetHandle()),
                nameof(LibUsb.Api.libusb_reset_device)
            );
    */
}
