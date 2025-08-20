using System;
using System.Threading;
using static LibUsbSharp.Internal.RundownGuard;

namespace LibUsbSharp.Internal;

public class RundownGuard : IRundownGuard
{
    private readonly int _maxSharedCount;
    private int _activeCount = 0;
    private bool _isShuttingDown = false;
    private bool _exclusiveHeld = false;
    private int _exclusiveWaiters = 0;
    private bool _rundownStarted = false;
    private bool _rundownCompleted = false;
    private readonly object _lock = new();

    public RundownGuard(int maxSharedCount = int.MaxValue)
    {
        if (maxSharedCount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(maxSharedCount),
                "Must be positive or int.MaxValue for unlimited."
            );

        _maxSharedCount = maxSharedCount;
    }

    public IDisposable? AcquireSharedToken(TimeSpan? timeout = null)
    {
        return !AcquireShared(timeout) ? null : (IDisposable)new ProtectionToken(this);
    }

    public bool AcquireShared(TimeSpan? timeout = null)
    {
        var deadline = timeout is null ? null : DateTime.UtcNow + timeout;

        lock (_lock)
        {
            while (true)
            {
                if (_isShuttingDown)
                {
                    return false;
                }

                if (!_exclusiveHeld && _exclusiveWaiters == 0 && _activeCount < _maxSharedCount)
                {
                    _activeCount++;
                    return true;
                }

                AquireLock(deadline);
            }
        }
    }

    public IDisposable? AcquireExclusiveToken(TimeSpan? timeout = null)
    {
        return !AcquireExclusive(timeout) ? null : (IDisposable)new ExclusiveToken(this);
    }

    public bool AcquireExclusive(TimeSpan? timeout = null)
    {
        var deadline = timeout is null ? null : DateTime.UtcNow + timeout;

        lock (_lock)
        {
            _exclusiveWaiters++;

            try
            {
                while (true)
                {
                    if (_isShuttingDown)
                    {
                        return false;
                    }

                    if (!_exclusiveHeld && _activeCount == 0)
                    {
                        _exclusiveHeld = true;
                        return true;
                    }

                    AquireLock(deadline);
                }
            }
            finally
            {
                _exclusiveWaiters--;
            }
        }
    }

    private void AquireLock(DateTime? deadline)
    {
        if (deadline is null)
        {
            _ = Monitor.Wait(_lock);
        }
        else
        {
            var remaining = deadline.Value.Subtract(DateTime.UtcNow);
            if (remaining <= TimeSpan.Zero || !Monitor.Wait(_lock, remaining))
                throw new TimeoutException();
        }
    }

    /// <summary>
    /// Initiates rundown. Only one thread performs rundown and receives a token.
    /// Other threads block until rundown completes, then throw RundownException.
    /// </summary>
    public void TriggerRundown()
    {
        lock (_lock)
        {
            _isShuttingDown = true;
        }
    }

    private void RundownComplete()
    {
        lock (_lock)
        {
            _rundownCompleted = true;
        }
    }

    /// <summary>
    /// Initiates rundown. Only one thread performs rundown and receives a token.
    /// Other threads block until rundown completes, then throw RundownException.
    /// </summary>
    public IDisposable? WaitForRundown()
    {
        lock (_lock)
        {
            if (_rundownCompleted)
                return null;

            if (_rundownStarted)
            {
                while (!_rundownCompleted)
                {
                    _ = Monitor.Wait(_lock);
                }
                return null;
            }

            _rundownStarted = true;
            _isShuttingDown = true;

            while (_activeCount > 0 || _exclusiveHeld)
            {
                _ = Monitor.Wait(_lock);
            }

            return new RundownToken(this);
        }
    }

    public void ReleaseShared()
    {
        lock (_lock)
        {
            _activeCount--;
            Monitor.PulseAll(_lock);
        }
    }

    public void ReleaseExclusive()
    {
        lock (_lock)
        {
            _exclusiveHeld = false;
            Monitor.PulseAll(_lock);
        }
    }

    public sealed class ProtectionToken : IDisposable
    {
        private RundownGuard? _owner;

        internal ProtectionToken(RundownGuard owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            if (_owner != null)
            {
                _owner.ReleaseShared();
                _owner = null;
            }
        }
    }

    public sealed class ExclusiveToken : IDisposable
    {
        private RundownGuard _owner;

        internal ExclusiveToken(RundownGuard owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            _owner.ReleaseExclusive();
        }
    }

    public sealed class RundownToken : IDisposable
    {
        private RundownGuard _owner;

        internal RundownToken(RundownGuard owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            _owner.RundownComplete();
        }
    }

    public class RundownException : ObjectDisposedException
    {
        public RundownException(string message)
            : base(message) { }
    }
}
