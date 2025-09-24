namespace LibUsbNative.SafeHandles;

public interface ISafeDeviceList : IReadOnlyList<ISafeDevice>, IDisposable
{
    bool IsClosed { get; }
}
