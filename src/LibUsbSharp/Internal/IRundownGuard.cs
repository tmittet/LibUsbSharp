using static LibUsbSharp.Internal.RundownGuard;

namespace LibUsbSharp.Internal;

public interface IRundownGuard
{
    ProtectionToken AcquireSharedToken(TimeSpan? timeout = null);
    void AcquireShared(TimeSpan? timeout = null);

    ExclusiveToken AcquireExclusiveToken(TimeSpan? timeout = null);
    void AcquireExclusive(TimeSpan? timeout = null);

    void ReleaseShared();
    void ReleaseExclusive();

    void TriggerRundown();
    IDisposable? WaitForRundown();
}
