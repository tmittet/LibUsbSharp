using static LibUsbSharp.Internal.RundownGuard;

namespace LibUsbSharp.Internal;

public interface IRundownGuard
{
    IDisposable? AcquireSharedToken(TimeSpan? timeout = null);
    bool AcquireShared(TimeSpan? timeout = null);

    IDisposable? AcquireExclusiveToken(TimeSpan? timeout = null);
    bool AcquireExclusive(TimeSpan? timeout = null);

    void ReleaseShared();
    void ReleaseExclusive();

    void TriggerRundown();
    IDisposable? WaitForRundown();
}
