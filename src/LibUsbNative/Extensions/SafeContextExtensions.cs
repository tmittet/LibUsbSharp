using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using LibUsbNative;
using LibUsbNative.SafeHandles;

namespace LibUsbNative.Extensions;

/*
public static class SafeContextExtensions
{
    private static IntPtr Raw(this SafeContext ctx)
    {
        if (ctx is null)
            throw new ArgumentNullException(nameof(ctx));
        return ctx.DangerousGetHandle();
    }

  
    // -------- Options / Debug --------
    public static int SetOption(this SafeContext ctx, LibusbOption option, IntPtr value)
    {
        var rc = Libusb.Api.libusb_set_option(ctx.Raw(), option, value);
        LibUsbException.ThrowIfError(rc, nameof(Libusb.Api.libusb_set_option));
        return rc;
    }
  
        public static void SetLogLevel(this SafeContext ctx, int level)
        {
            // Prefer set_option(LOG_LEVEL) where available
            var rc = Libusb.Api.libusb_set_option(ctx.Raw(), LibusbOption.LOG_LEVEL, (IntPtr)level);
            LibUsbException.ThrowIfError(rc, nameof(Libusb.Api.libusb_set_option));
        }
  
    // Fallback / legacy
    //public static void SetDebug(this SafeContext ctx, int level) => Libusb.Api.libusb_set_debug(ctx.Raw(), level);

    // -------- Version --------
    public static string GetLibraryVersionDescription(this SafeContext _)
    {
        // libusb_version layout (major, minor, micro, nano are uint16; rc, describe are const char*)
        var p = Libusb.Api.libusb_get_version();
        if (p == IntPtr.Zero)
            return string.Empty;

        // Offsets per struct (assuming packing = default)
        ushort ReadU16(int off) => (ushort)Marshal.ReadInt16(p, off);
        var major = ReadU16(0);
        var minor = ReadU16(2);
        var micro = ReadU16(4);
        var nano = ReadU16(6);

        // Pointers follow (native pointer size). After 8 bytes of 4x uint16.
        var ptrSize = IntPtr.Size;
        var rcPtr = Marshal.ReadIntPtr(p, 8);
        var describePtr = Marshal.ReadIntPtr(p, 8 + ptrSize);

        string PtrToString(IntPtr sp) => sp == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(sp) ?? string.Empty;

        var rcStr = PtrToString(rcPtr);
        var desc = PtrToString(describePtr);

        return $"libusb {major}.{minor}.{micro}.{nano}"
            + (string.IsNullOrEmpty(rcStr) ? "" : $" ({rcStr})")
            + (string.IsNullOrEmpty(desc) ? "" : $" - {desc}");
    }

    
    public static (SafeDeviceList list, long count) GetDeviceList(this SafeContext ctx)
    {
        return SafeDeviceList.Get(ctx.DangerousGetHandle());
    }

    // -------- Event Handling --------
    public static void HandleEvents(this SafeContext ctx)
    {
        var rc = Libusb.Api.libusb_handle_events(ctx.Raw());
        LibUsbException.ThrowIfError(rc, nameof(Libusb.Api.libusb_handle_events));
    }

    public static void HandleEvents(this SafeContext ctx, TimeSpan timeout)
    {
        var tv = ToTimeVal(timeout);
        var rc = Libusb.Api.libusb_handle_events_timeout(ctx.Raw(), ref tv);
        LibUsbException.ThrowIfError(rc, nameof(Libusb.Api.libusb_handle_events_timeout));
    }

    public static void HandleEventsUntilCompleted(this SafeContext ctx, TimeSpan timeout, ref bool completed)
    {
        var arr = new int[1] { completed ? 1 : 0 };
        var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
        try
        {
            var tv = ToTimeVal(timeout);
            var rc = Libusb.Api.libusb_handle_events_timeout_completed(ctx.Raw(), ref tv, handle.AddrOfPinnedObject());
            LibUsbException.ThrowIfError(rc, nameof(Libusb.Api.libusb_handle_events_timeout_completed));
            completed = arr[0] != 0;
        }
        finally
        {
            handle.Free();
        }
    }

    // -------- Poll FDs --------
    public static IntPtr GetPollFds(this SafeContext ctx, out IntPtr pollFdsPtr) =>
        Libusb.Api.libusb_get_pollfds(ctx.Raw(), out pollFdsPtr);

    public static void FreePollFds(this SafeContext ctx, IntPtr pollFdsPtr) =>
        Libusb.Api.libusb_free_pollfds(pollFdsPtr);

    public static int RegisterLogCallback(this SafeContext ctx, Action<int, string> logHandler)
    {
        if (logHandler is null)
            throw new ArgumentNullException(nameof(logHandler));

        void LibUsbLogHandler(IntPtr ptr, int level, string messagePtr)
        {
            //var msg = Marshal.PtrToStringAnsi(messagePtr) ?? string.Empty;
            logHandler(level, messagePtr);
        }
        // Keep the delegate alive

        var callback = new libusb_log_callback(LibUsbLogHandler);
        GCHandle.Alloc(callback);

        return SetOption(ctx, LibusbOption.LIBUSB_OPTION_LOG_CB, Marshal.GetFunctionPointerForDelegate(callback));
    }

    // -------- Hotplug --------
    public static int HotplugRegisterCallback(
        this SafeContext ctx,
        int events,
        int flags,
        int vendorId,
        int productId,
        int deviceClass,
        libusb_hotplug_callback_fn callback,
        IntPtr userData = default
    )
    {
        var rc = Libusb.Api.libusb_hotplug_register_callback(
            ctx.Raw(),
            events,
            flags,
            vendorId,
            productId,
            deviceClass,
            callback,
            userData,
            out var cbHandle
        );
        LibUsbException.ThrowIfError(rc, nameof(Libusb.Api.libusb_hotplug_register_callback));
        return cbHandle;
    }

    public static void HotplugDeregisterCallback(this SafeContext ctx, int callbackHandle) =>
        Libusb.Api.libusb_hotplug_deregister_callback(ctx.Raw(), callbackHandle);

    // -------- Helpers --------
    private static TimeVal ToTimeVal(TimeSpan ts)
    {
        if (ts < TimeSpan.Zero)
            ts = TimeSpan.Zero;
        long totalMicro = (long)(ts.TotalMilliseconds * 1000.0);
        return new TimeVal { tv_sec = (IntPtr)(totalMicro / 1_000_000), tv_usec = (IntPtr)(totalMicro % 1_000_000) };
    }
}
*/
