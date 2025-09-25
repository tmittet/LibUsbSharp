using LibUsbNative.Enums;

namespace LibUsbNative.SafeHandles;

public interface ISafeContext : IDisposable
{
    void RegisterLogCallback(Action<int, string> logHandler);

    nint HotplugRegisterCallback(
        int events,
        int flags,
        int vendorId,
        int productId,
        int deviceClass,
        nint userData,
        Func<ISafeContext, ISafeDevice, int, nint, bool> hotPlugCallback
    );

    void HotplugDeregisterCallback(nint callbackHandle);

    void SetOption(libusb_option libusbOption, int value);

    void SetOption(libusb_option libusbOption, nint value);

    libusb_error HandleEventsCompleted(nint completedPtr);

    void InterruptEventHandler();

    ISafeDeviceList GetDeviceList();
}
