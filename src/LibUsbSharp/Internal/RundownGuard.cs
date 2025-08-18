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

    public ProtectionToken AcquireSharedToken(TimeSpan? timeout = null)
    {
        AcquireShared(timeout);
        return new ProtectionToken(this);
    }

    //public void AcquireShared(TimeSpan? timeout = null, Func<int>? func = null)
    public void AcquireShared(TimeSpan? timeout = null)
    {
        DateTime? deadline = (DateTime?)(timeout != null ? DateTime.UtcNow + timeout! : null);

        lock (_lock)
        {
            while (true)
            {
                if (_isShuttingDown)
                {
                    throw new RundownException("Rundown initiated");
                }
                if (_exclusiveHeld || _exclusiveWaiters > 0 || _activeCount >= _maxSharedCount)
                {
                    if (deadline is not null)
                    {
                        var remaining = (TimeSpan)(deadline! - DateTime.UtcNow);
                        if (remaining <= TimeSpan.Zero || !Monitor.Wait(_lock, remaining))
                            throw new TimeoutException();
                    }
                    else
                    {
                        _ = Monitor.Wait(_lock);
                    }

                    continue;
                }

                _activeCount++;
                return;
            }
        }
    }

    public ExclusiveToken AcquireExclusiveToken(TimeSpan? timeout = null)
    {
        AcquireExclusive(timeout);
        return new ExclusiveToken(this);
    }

    public void AcquireExclusive(TimeSpan? timeout = null)
    {
        DateTime? deadline = (DateTime?)(timeout != null ? DateTime.UtcNow + timeout! : null);

        lock (_lock)
        {
            _exclusiveWaiters++;

            try
            {
                while (true)
                {
                    if (_isShuttingDown)
                    {
                        throw new RundownException("Rundown initiated");
                    }

                    if (_exclusiveHeld || _activeCount > 0)
                    {
                        if (deadline is not null)
                        {
                            var remaining = (TimeSpan)(deadline! - DateTime.UtcNow);
                            if (remaining <= TimeSpan.Zero || !Monitor.Wait(_lock, remaining))
                                throw new TimeoutException();
                        }
                        else
                        {
                            _ = Monitor.Wait(_lock);
                        }
                        continue;
                    }
                    _exclusiveHeld = true;
                    return;
                }
            }
            finally
            {
                _exclusiveWaiters--;
            }
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
                    Monitor.Wait(_lock);
                }
                return null;
            }

            _rundownStarted = true;
            _isShuttingDown = true;

            while (_activeCount > 0 || _exclusiveHeld)
            {
                Monitor.Wait(_lock);
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
