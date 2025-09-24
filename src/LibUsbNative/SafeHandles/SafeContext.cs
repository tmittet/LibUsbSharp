using System.Runtime.InteropServices;
using LibUsbNative.Enums;

namespace LibUsbNative.SafeHandles;

internal sealed class SafeContext : SafeHandle, ISafeContext
{
    internal readonly ILibUsbApi api;

    public SafeContext(ILibUsbApi api)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        var result = api.libusb_init(out var raw);
        if (result != 0 || raw == IntPtr.Zero)
        {
            throw new LibUsbException(result, "Failed to initialize libusb context.");
        }

        this.api = api;
        SetHandle(raw);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
            return true;

        api.libusb_exit(handle);
        return true;
    }

    public void SetOption(libusb_option option, int value)
    {
        SafeHelpers.ThrowIfClosed(this);

        var rc = api.libusb_set_option(handle, option, value);
        LibUsbException.ThrowIfError(rc, "libusb_set_option");
    }

    public void SetOption(libusb_option option, IntPtr value)
    {
        SafeHelpers.ThrowIfClosed(this);

        var rc = api.libusb_set_option(handle, option, value);
        LibUsbException.ThrowIfError(rc, "libusb_set_option");
    }

    public libusb_error HandleEventsCompleted(IntPtr completedPtr)
    {
        SafeHelpers.ThrowIfClosed(this);

        return completedPtr == IntPtr.Zero
            ? throw new ArgumentNullException(nameof(completedPtr))
            : api.libusb_handle_events_completed(handle, completedPtr);
    }

    public void InterruptEventHandler()
    {
        SafeHelpers.ThrowIfClosed(this);

        api.libusb_interrupt_event_handler(handle);
    }

    public void RegisterLogCallback(Action<int, string> logHandler)
    {
        SafeHelpers.ThrowIfClosed(this);

        ArgumentNullException.ThrowIfNull(logHandler);

        void LibUsbLogHandler(IntPtr ptr, int level, string messagePtr)
        {
            logHandler(level, messagePtr);
        }

        var callback = new libusb_log_callback(LibUsbLogHandler);
        _ = GCHandle.Alloc(callback);

        try
        {
            SetOption(libusb_option.LIBUSB_OPTION_LOG_CB, Marshal.GetFunctionPointerForDelegate(callback));
        }
        catch
        {
            GCHandle.FromIntPtr(Marshal.GetFunctionPointerForDelegate(callback)).Free();
            throw;
        }
    }

    public IntPtr HotplugRegisterCallback(
        int events,
        int flags,
        int vendorId,
        int productId,
        int deviceClass,
        IntPtr userData,
        Func<ISafeContext, ISafeDevice, int, IntPtr, bool> hotPlugCallback
    )
    {
        SafeHelpers.ThrowIfClosed(this);
        ArgumentNullException.ThrowIfNull(hotPlugCallback);

        int InternalCallback(IntPtr ctx, IntPtr dev, int eventType, IntPtr userData)
        {
            return hotPlugCallback(this, new SafeDevice(this, dev), eventType, userData) ? 1 : 0;
        }
        var callback = new libusb_hotplug_callback_fn(InternalCallback);
        _ = GCHandle.Alloc(callback);

        var result = api.libusb_hotplug_register_callback(
            handle,
            events,
            flags,
            vendorId,
            productId,
            deviceClass,
            callback,
            userData,
            out var callbackHandle
        );

        LibUsbException.ThrowIfError(result, "Failed to register hotplug callback");

        return (IntPtr)callbackHandle;
    }

    public void HotplugDeregisterCallback(IntPtr callbackHandle)
    {
        SafeHelpers.ThrowIfClosed(this);

        if (callbackHandle == IntPtr.Zero)
            throw new ArgumentNullException(nameof(callbackHandle));

        api.libusb_hotplug_deregister_callback(handle, callbackHandle);
    }

    // TODO: Using out parameter for count or returning List would be clearer
    public ISafeDeviceList GetDeviceList()
    {
        SafeHelpers.ThrowIfClosed(this);

        var rc = api.libusb_get_device_list(handle, out var list);
        LibUsbException.ThrowIfError(rc, "Failed to get device list");

        var success = false;
        DangerousAddRef(ref success);
        if (!success)
        {
            api.libusb_free_device_list(list, 1);
            LibUsbException.ThrowIfError(libusb_error.LIBUSB_ERROR_OTHER, "Failed to ref SafeHandle");
        }

        return new SafeDeviceList(this, list, (int)rc);
    }
}
