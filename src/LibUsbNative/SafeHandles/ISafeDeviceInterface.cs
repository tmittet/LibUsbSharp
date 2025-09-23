namespace LibUsbNative.SafeHandles;

public interface ISafeDeviceInterface : IDisposable
{
    int GetInterfaceNumber();
}
