namespace LibUsbSharp.Native.SafeHandles;

public interface ISafeDeviceList : IReadOnlyList<ISafeDevice>, IDisposable
{
    bool IsClosed { get; }
}
