using System.Runtime.InteropServices;
using LibUsbSharp.Native.Enums;
using LibUsbSharp.Native.Functions;

namespace LibUsbSharp.Native.SafeHandles;

internal sealed class SafeContext : SafeHandle, ISafeContext
{
    private int _logCallbackRegistered;
    private GCHandle? _logCallbackHandle;

    internal ILibUsbApi Api { get; init; }

    public override bool IsInvalid => handle == IntPtr.Zero;

    public SafeContext(ILibUsbApi api)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        var result = api.libusb_init(out var rawHandle);
        if (result != 0 || rawHandle == IntPtr.Zero)
        {
            throw LibUsbException.FromApiError(result, nameof(Api.libusb_init));
        }
        Api = api;
        handle = rawHandle;
    }

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
        SafeHelper.ThrowIfClosed(this);
        var result = Api.libusb_set_option(handle, libusbOption, value);
        LibUsbException.ThrowIfApiError(result, nameof(Api.libusb_set_option));
    }

    /// <inheritdoc />
    public void SetOption(libusb_option libusbOption, nint value)
    {
        SafeHelper.ThrowIfClosed(this);
        var result = Api.libusb_set_option(handle, libusbOption, value);
        LibUsbException.ThrowIfApiError(result, nameof(Api.libusb_set_option));
    }

    /// <inheritdoc />
    public libusb_error HandleEventsCompleted(nint completedPtr)
    {
        SafeHelper.ThrowIfClosed(this);

        return completedPtr == IntPtr.Zero // TODO: Zero pointer OK according to libusb docs.
            ? throw new ArgumentNullException(nameof(completedPtr))
            : Api.libusb_handle_events_completed(handle, completedPtr);
    }

    /// <inheritdoc />
    public void InterruptEventHandler()
    {
        SafeHelper.ThrowIfClosed(this);
        Api.libusb_interrupt_event_handler(handle);
    }

    /// <inheritdoc />
    public void RegisterLogCallback(Action<libusb_log_level, string> logHandler)
    {
        SafeHelper.ThrowIfClosed(this);
        ArgumentNullException.ThrowIfNull(logHandler);

        if (Interlocked.CompareExchange(ref _logCallbackRegistered, 1, 0) != 0)
            throw new InvalidOperationException("Log callback is already registered.");

        var callback = new libusb_log_cb((_, level, message) => logHandler(level, message));
        _logCallbackHandle = GCHandle.Alloc(callback);
        SetOption(libusb_option.LIBUSB_OPTION_LOG_CB, Marshal.GetFunctionPointerForDelegate(callback));
    }

    /// <inheritdoc />
    public ISafeCallbackHandle RegisterHotplugCallback(
        libusb_hotplug_event events,
        libusb_hotplug_flag flags,
        Func<ISafeContext, ISafeDevice, libusb_hotplug_event, libusb_hotplug_return> callback,
        libusb_class_code? deviceClass,
        ushort? vendorId,
        ushort? productId
    ) =>
        RegisterHotplugCallback(
            events,
            flags,
            (context, device, eventType, _) => callback(context, device, eventType),
            IntPtr.Zero,
            deviceClass,
            vendorId,
            productId
        );

    /// <inheritdoc />
    public ISafeCallbackHandle RegisterHotplugCallback(
        libusb_hotplug_event events,
        libusb_hotplug_flag flags,
        Func<ISafeContext, ISafeDevice, libusb_hotplug_event, nint, libusb_hotplug_return> callback,
        nint userData,
        libusb_class_code? deviceClass,
        ushort? vendorId,
        ushort? productId
    )
    {
        const int HotPlugMatchAny = -1;

        SafeHelper.ThrowIfClosed(this);
        ArgumentNullException.ThrowIfNull(callback);

        // Create hotplug hotplugCallback with a pinned handle
        var hotplugCallback = new libusb_hotplug_callback_fn(
            (_, dev, eventType, userData) => TriggerExternalCallback(dev, eventType, userData, callback)
        );
        var gcHandle = GCHandle.Alloc(hotplugCallback, GCHandleType.Pinned);

        // Register hotplug hotplugCallback
        var result = Api.libusb_hotplug_register_callback(
            handle,
            events,
            flags,
            vendorId ?? HotPlugMatchAny,
            productId ?? HotPlugMatchAny,
            deviceClass is null ? HotPlugMatchAny : (int)deviceClass,
            hotplugCallback,
            userData,
            out var callbackHandle
        );
        if (result is not libusb_error.LIBUSB_SUCCESS)
        {
            gcHandle.Free();
        }
        LibUsbException.ThrowIfApiError(result, nameof(Api.libusb_hotplug_register_callback));

        // Increment context reference counter, SafeHotplugCallbackHandle will decrement on dispose
        var success = false;
        DangerousAddRef(ref success);
        if (!success)
        {
            Api.libusb_hotplug_deregister_callback(handle, callbackHandle);
            gcHandle.Free();
            throw LibUsbException.FromError(libusb_error.LIBUSB_ERROR_OTHER, "Failed to ref SafeHandle.");
        }

        // Create and return SafeHotplugCallbackHandle that deregister hotplugCallback and decrements ref counter on release
        return new SafeHotplugCallbackHandle(this, gcHandle, callbackHandle);
    }

    private libusb_hotplug_return TriggerExternalCallback(
        nint devicePtr,
        libusb_hotplug_event eventType,
        nint userData,
        Func<ISafeContext, ISafeDevice, libusb_hotplug_event, nint, libusb_hotplug_return> callback
    )
    {
        var success = false;
        // Increment context ref counter, SafeDevice will decrement it on dispose.
        DangerousAddRef(ref success);
        return success
            ? callback(this, new SafeDevice(this, devicePtr), eventType, userData)
            : throw LibUsbException.FromError(libusb_error.LIBUSB_ERROR_OTHER, "Failed to ref SafeContext handle.");
    }

    /// <inheritdoc />
    public ISafeDeviceList GetDeviceList()
    {
        SafeHelper.ThrowIfClosed(this);

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
