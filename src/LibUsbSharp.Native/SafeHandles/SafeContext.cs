using System.Runtime.InteropServices;
using LibUsbSharp.Native.Enums;

namespace LibUsbSharp.Native.SafeHandles;

internal sealed class SafeContext : SafeHandle, ISafeContext
{
    private int _logCallbackRegistered;
    private GCHandle? _logCallbackHandle;

    internal ILibUsbApi Api { get; init; }

    public SafeContext(ILibUsbApi api)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        var result = api.libusb_init(out var raw);
        if (result != 0 || raw == IntPtr.Zero)
        {
            throw LibUsbException.FromApiError(result, nameof(Api.libusb_init));
        }
        SetHandle(raw);
        Api = api;
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
            return true;

        Api.libusb_exit(handle);
        _logCallbackHandle?.Free();
        return true;
    }

    /// <inheritdoc />
    public void SetOption(libusb_option libusbOption, int value)
    {
        SafeHelpers.ThrowIfClosed(this);
        var result = Api.libusb_set_option(handle, libusbOption, value);
        LibUsbException.ThrowIfApiError(result, nameof(Api.libusb_set_option));
    }

    /// <inheritdoc />
    public void SetOption(libusb_option libusbOption, nint value)
    {
        SafeHelpers.ThrowIfClosed(this);
        var result = Api.libusb_set_option(handle, libusbOption, value);
        LibUsbException.ThrowIfApiError(result, nameof(Api.libusb_set_option));
    }

    /// <inheritdoc />
    public libusb_error HandleEventsCompleted(nint completedPtr)
    {
        SafeHelpers.ThrowIfClosed(this);

        return completedPtr == IntPtr.Zero // TODO: Zero pointer OK according to libusb docs.
            ? throw new ArgumentNullException(nameof(completedPtr))
            : Api.libusb_handle_events_completed(handle, completedPtr);
    }

    /// <inheritdoc />
    public void InterruptEventHandler()
    {
        SafeHelpers.ThrowIfClosed(this);
        Api.libusb_interrupt_event_handler(handle);
    }

    /// <inheritdoc />
    public void RegisterLogCallback(Action<libusb_log_level, string> logHandler)
    {
        SafeHelpers.ThrowIfClosed(this);
        ArgumentNullException.ThrowIfNull(logHandler);

        if (Interlocked.CompareExchange(ref _logCallbackRegistered, 1, 0) != 0)
            throw new InvalidOperationException("Log callback is already registered.");

        var callback = new libusb_log_callback((_, level, message) => logHandler(level, message));
        _logCallbackHandle = GCHandle.Alloc(callback);
        SetOption(libusb_option.LIBUSB_OPTION_LOG_CB, Marshal.GetFunctionPointerForDelegate(callback));
    }

    /// <inheritdoc />
    public nint HotplugRegisterCallback(
        libusb_hotplug_event events,
        libusb_hotplug_flag flags,
        ushort? vendorId,
        ushort? productId,
        libusb_class_code? deviceClass,
        nint userData,
        Func<ISafeContext, ISafeDevice, libusb_hotplug_event, nint, libusb_hotplug_return> hotplugCallback
    )
    {
        const int HotPlugMatchAny = -1;

        SafeHelpers.ThrowIfClosed(this);
        ArgumentNullException.ThrowIfNull(hotplugCallback);

        var callback = new libusb_hotplug_callback_fn(
            (_, dev, eventType, userData) => hotplugCallback(this, new SafeDevice(this, dev), eventType, userData)
        );
        var gcHandle = GCHandle.Alloc(callback);

        var result = Api.libusb_hotplug_register_callback(
            handle,
            events,
            flags,
            vendorId ?? HotPlugMatchAny,
            productId ?? HotPlugMatchAny,
            deviceClass is null ? HotPlugMatchAny : (int)deviceClass,
            callback,
            userData,
            out var callbackHandle
        );

        try
        {
            // TODO: On success GCHandle is never freed in this implementation
            LibUsbException.ThrowIfApiError(result, nameof(Api.libusb_hotplug_register_callback));
        }
        catch
        {
            gcHandle.Free();
            throw;
        }

        return callbackHandle;
    }

    /// <inheritdoc />
    public void HotplugDeregisterCallback(nint callbackHandle)
    {
        SafeHelpers.ThrowIfClosed(this);

        if (callbackHandle == IntPtr.Zero)
            throw new ArgumentNullException(nameof(callbackHandle));

        Api.libusb_hotplug_deregister_callback(handle, callbackHandle);
    }

    /// <inheritdoc />
    public ISafeDeviceList GetDeviceList()
    {
        SafeHelpers.ThrowIfClosed(this);

        var rc = Api.libusb_get_device_list(handle, out var list);
        LibUsbException.ThrowIfApiError(rc, nameof(Api.libusb_get_device_list));

        var success = false;
        DangerousAddRef(ref success);
        if (!success)
        {
            Api.libusb_free_device_list(list, 1);
            throw LibUsbException.FromError(libusb_error.LIBUSB_ERROR_OTHER, "Failed to ref SafeHandle.");
        }

        return new SafeDeviceList(this, list, (int)rc);
    }
}
