namespace LibUsbNative.SafeHandles;

public interface ISafeDeviceList : IDisposable
{
    IEnumerable<ISafeDevice> Devices { get; }
    bool IsClosed { get; }
}
