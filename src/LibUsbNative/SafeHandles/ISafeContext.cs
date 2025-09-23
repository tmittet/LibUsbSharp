using LibUsbNative.Enums;

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

    void SetOption(LibUsbOption opt, int value);
    void SetOption(LibUsbOption option, IntPtr value);
    LibUsbError HandleEventsCompleted(IntPtr param);
    void InterruptEventHandler();
    (ISafeDeviceList, uint) GetDeviceList();
}
