using System.Runtime.InteropServices;

namespace LibUsbNative.SafeHandles;

public interface ISafeContext : IDisposable
{
    void RegisterLogCallback(Action<int, string> logHandler);
    IntPtr HotplugRegisterCallback(
        int events,
        int flags,
        int vendorId,
        int productId,
        int deviceClass,
        IntPtr userData,
        Func<ISafeContext, ISafeDevice, int, IntPtr, bool> hotPlugCallback
    );
    void HotplugDeregisterCallback(IntPtr callbackHandle);

    void SetOption(LibusbOption opt, int value);
    void SetOption(LibusbOption option, IntPtr value);
    LibUsbError HandleEventsCompleted(IntPtr param);
    void InterruptEventHandler();
    (ISafeDeviceList, uint) GetDeviceList();
}

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

    public void SetOption(int option, int value)
    {
        SafeHelpers.ThrowIfClosed(this);
        SetOption((LibusbOption)option, value);
    }

    public void SetOption(LibusbOption option, int value)
    {
        SafeHelpers.ThrowIfClosed(this);

        var rc = api.libusb_set_option(handle, option, value);
        LibUsbException.ThrowIfError(rc, "libusb_set_option");
    }

    public void SetOption(LibusbOption option, IntPtr value)
    {
        SafeHelpers.ThrowIfClosed(this);

        var rc = api.libusb_set_option(handle, option, value);
        LibUsbException.ThrowIfError(rc, "libusb_set_option");
    }

    public LibUsbError HandleEventsCompleted(IntPtr completedPtr)
    {
        SafeHelpers.ThrowIfClosed(this);

        if (completedPtr == IntPtr.Zero)
            throw new ArgumentNullException(nameof(completedPtr));

        return api.libusb_handle_events_completed(handle, completedPtr);
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
        GCHandle.Alloc(callback);

        try
        {
            SetOption(LibusbOption.LIBUSB_OPTION_LOG_CB, Marshal.GetFunctionPointerForDelegate(callback));
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

        if (hotPlugCallback is null)
            throw new ArgumentNullException(nameof(hotPlugCallback));

        int InternalCallback(IntPtr ctx, IntPtr dev, int eventType, IntPtr userData)
        {
            return hotPlugCallback(this, new SafeDevice(this, dev), eventType, userData) ? 1 : 0;
        }
        var callback = new libusb_hotplug_callback_fn(InternalCallback);
        GCHandle.Alloc(callback);

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

    public (ISafeDeviceList, uint) GetDeviceList()
    {
        SafeHelpers.ThrowIfClosed(this);

        var rc = api.libusb_get_device_list(handle, out var list);
        LibUsbException.ThrowIfError(rc, "Failed to get device list");

        bool success = false;
        DangerousAddRef(ref success);
        if (!success)
        {
            api.libusb_free_device_list(list, 1);
            LibUsbException.ThrowIfError(LibUsbError.Other, "Failed to ref SafeHandle");
        }

        return (new SafeDeviceList(this, list, (uint)rc), (uint)rc);
    }
}
