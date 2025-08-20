//
// RundownGuard
//
// Purpose
// - Coordinates concurrent work (shared access) and critical operations (exclusive access).
// - Enables a cooperative "rundown" phase that:
//   1) Prevents new work from starting,
//   2) Waits for in-flight work to finish,
//   3) Allows exactly one thread to perform final teardown,
//   4) Lets all other threads block until rundown is completed.
//
// When to use
// - Managing lifecycle-sensitive resources where you must stop accepting new work,
//   wait for current work to drain, then perform an exclusive teardown (e.g., device/handle
//   teardown, I/O pipeline shutdown, background processing stop).
//
// Key concepts
// - Shared: Many concurrent holders (up to maxSharedCount) when no exclusive holder exists.
// - Exclusive: Single holder with no concurrent shared holders.
// - Rundown: A one-time shutdown gate; once started, new acquisitions fail, and one thread
//   is given a rundown token to perform teardown while others wait for completion.
//
// Notes
// - Acquire* methods can take an optional timeout; if it elapses, a TimeoutException is thrown.
// - If shutdown has started, Acquire* methods fail (return false/null).
// - Always dispose returned tokens to release the corresponding hold.
//

using System;
using System.Diagnostics;
using System.Threading;
using static LibUsbSharp.Internal.RundownGuard;

namespace LibUsbSharp.Internal;

/// <summary>
/// Coordinates shared and exclusive access and provides a one-time "rundown" (shutdown) mechanism.
/// </summary>
/// <remarks>
/// Typical usage:
/// - Normal operation: acquire shared tokens for work, occasionally exclusive tokens for critical updates.
/// - Shutdown: call <see cref="TriggerRundown"/> (optional), then call <see cref="WaitForRundown"/>.
///   The first caller to <see cref="WaitForRundown"/> becomes the teardown owner and receives a
///   <see cref="RundownToken"/>. All other callers block until rundown completes and then return null.
/// </remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// // Normal usage (shared work)
/// var guard = new RundownGuard();
/// using (guard.AcquireSharedToken()!)
/// {
///     // Do concurrent work protected by the guard
/// }
///
/// // Exclusive usage (no shared holders present)
/// using (guard.AcquireExclusiveToken()!)
/// {
///     // Perform critical operation that must not overlap with shared work
/// }
///
/// // Shutdown / Rundown
/// guard.TriggerRundown(); // Optional: proactively prevent new acquisitions
/// using (var token = guard.WaitForRundown())
/// {
///     if (token != null)
///     {
///         // This thread owns rundown; at this point no shared or exclusive holders exist.
///         // Perform teardown here (dispose handles, stop threads, etc.)
///     }
///     // If token is null, either rundown already completed, or another thread is performing it.
/// }
/// ]]></code>
/// </example>
public class RundownGuard
{
    // Maximum number of concurrent shared holders allowed.
    private readonly int _maxSharedCount;

    // Number of currently active shared holders.
    private int _activeCount;

    // Indicates shutdown intent; once true, no new acquisitions are allowed.
    private bool _isShuttingDown;

    // True while an exclusive token is held.
    private bool _exclusiveHeld;

    // Number of threads currently waiting for exclusive access.
    private int _exclusiveWaiters;

    // Rundown state flags: started and completed.
    private bool _rundownStarted;
    private bool _rundownCompleted ;

    // Intrinsic lock for all condition changes.
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new <see cref="RundownGuard"/>.
    /// </summary>
    /// <param name="maxSharedCount">
    /// Maximum number of concurrent shared holders allowed; use <see cref="int.MaxValue"/> for unlimited.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="maxSharedCount"/> is not positive.</exception>
    public RundownGuard(int maxSharedCount = int.MaxValue)
    {
        if (maxSharedCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSharedCount),
                "Must be positive or int.MaxValue for unlimited."
            );
        }

        _maxSharedCount = maxSharedCount;
    }

    /// <summary>
    /// Attempts to acquire a shared token. Returns null if shutdown has started.
    /// </summary>
    /// <param name="timeout">Optional timeout. If expired, a <see cref="TimeoutException"/> is thrown.</param>
    /// <returns>An <see cref="IDisposable"/> token that must be disposed to release the shared hold; null if shutting down.</returns>
    /// <exception cref="TimeoutException">Thrown if the wait exceeds <paramref name="timeout"/>.</exception>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// using var token = guard.AcquireSharedToken(TimeSpan.FromSeconds(1));
    /// if (token is null)
    ///     return; // Shutting down
    ///
    /// // Do work under shared protection
    /// ]]></code>
    /// </example>
    public IDisposable? AcquireSharedToken(TimeSpan? timeout = null)
    {
        return AcquireShared(timeout) ? (IDisposable)new ProtectionToken(this) : null;
    }

    /// <summary>
    /// Attempts to acquire shared access. Returns false if shutdown has started.
    /// </summary>
    /// <param name="timeout">Optional timeout. If expired, a <see cref="TimeoutException"/> is thrown.</param>
    /// <returns>True if acquired; false if shutting down.</returns>
    /// <exception cref="TimeoutException">Thrown if the wait exceeds <paramref name="timeout"/>.</exception>
    public bool AcquireShared(TimeSpan? timeout = null)
    {
        // Start a stopwatch once so we can compute remaining time without DateTime.
        var sw = timeout is null ? null : Stopwatch.StartNew();

        lock (_lock)
        {
            while (true)
            {
                // If rundown/shutdown is in progress, reject new shared acquisitions.
                if (_isShuttingDown)
                {
                    return false;
                }

                // Allow shared acquisition only when:
                // - No exclusive holder is present,
                // - No exclusive waiter exists (to avoid starvation),
                // - Shared count has not reached the limit.
                if (!_exclusiveHeld && _exclusiveWaiters == 0 && _activeCount < _maxSharedCount)
                {
                    _activeCount++;
                    return true;
                }

                // Otherwise, wait until a state change occurs or timeout elapses.
                AquireLock(sw, timeout);
            }
        }
    }

    /// <summary>
    /// Attempts to acquire an exclusive token. Returns null if shutdown has started.
    /// </summary>
    /// <param name="timeout">Optional timeout. If expired, a <see cref="TimeoutException"/> is thrown.</param>
    /// <returns>An <see cref="IDisposable"/> token that must be disposed to release the exclusive hold; null if shutting down.</returns>
    /// <exception cref="TimeoutException">Thrown if the wait exceeds <paramref name="timeout"/>.</exception>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// using var token = guard.AcquireExclusiveToken(TimeSpan.FromSeconds(5));
    /// if (token is null)
    ///     return; // Shutting down
    ///
    /// // Perform critical operation under exclusive protection
    /// ]]></code>
    /// </example>
    public IDisposable? AcquireExclusiveToken(TimeSpan? timeout = null)
    {
        return AcquireExclusive(timeout) ? (IDisposable)new ExclusiveToken(this) : null;
    }

    /// <summary>
    /// Attempts to acquire exclusive access. Returns false if shutdown has started.
    /// </summary>
    /// <param name="timeout">Optional timeout. If expired, a <see cref="TimeoutException"/> is thrown.</param>
    /// <returns>True if acquired; false if shutting down.</returns>
    /// <exception cref="TimeoutException">Thrown if the wait exceeds <paramref name="timeout"/>.</exception>
    public bool AcquireExclusive(TimeSpan? timeout = null)
    {
        // Start a stopwatch once so we can compute remaining time without DateTime.
        var sw = timeout is null ? null : Stopwatch.StartNew();

        lock (_lock)
        {
            // Register that we're waiting for exclusivity so shared acquisitions don't starve us.
            _exclusiveWaiters++;

            try
            {
                while (true)
                {
                    // If rundown/shutdown is in progress, reject new acquisitions.
                    if (_isShuttingDown)
                    {
                        return false;
                    }

                    // Acquire exclusivity only when no shared holders are active and no exclusive holder exists.
                    if (!_exclusiveHeld && _activeCount == 0)
                    {
                        _exclusiveHeld = true;
                        return true;
                    }

                    // Otherwise, wait for a state change or timeout.
                    AquireLock(sw, timeout);
                }
            }
            finally
            {
                _exclusiveWaiters--;
            }
        }
    }

    // Wait on the lock with an optional timeout based on Stopwatch. Throws TimeoutException if the timeout elapses.
    private void AquireLock(Stopwatch? stopwatch, TimeSpan? timeout)
    {
        if (timeout is null)
        {
            _ = Monitor.Wait(_lock);
        }
        else
        {
            var remaining = timeout.Value - stopwatch!.Elapsed;
            if (remaining <= TimeSpan.Zero || !Monitor.Wait(_lock, remaining))
                throw new TimeoutException();
        }
    }

    /// <summary>
    /// Signals that shutdown should begin by preventing new acquisitions.
    /// Does not block. For teardown, call <see cref="WaitForRundown"/>.
    /// </summary>
    /// <remarks>
    /// This is optional; calling <see cref="WaitForRundown"/> will also initiate shutdown the first time.
    /// </remarks>
    public void TriggerRundown()
    {
        lock (_lock)
        {
            _isShuttingDown = true;
            // Wake any waiters so they can observe the shutdown state.
            Monitor.PulseAll(_lock);
        }
    }

    // Marks rundown completed and wakes any threads waiting for completion.
    private void RundownComplete()
    {
        lock (_lock)
        {
            _rundownCompleted = true;
            // Wake threads blocked in WaitForRundown() waiting for completion.
            Monitor.PulseAll(_lock);
        }
    }

    /// <summary>
    /// Waits for rundown to start or completes it.
    /// </summary>
    /// <returns>
    /// A <see cref="RundownToken"/> if this caller owns rundown and should perform teardown; otherwise null
    /// (either rundown is already in progress and this caller will block until completion, or rundown already completed).
    /// </returns>
    /// <remarks>
    /// - The first caller that observes rundown not yet started becomes the owner:
    ///   it sets shutdown, waits for all holders to drain, and receives a token for teardown.
    /// - All subsequent callers block until rundown is completed, then return null.
    /// - Dispose the returned token to signal completion and release waiters.
    /// </remarks>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// // Initiate rundown (optional)
    /// guard.TriggerRundown();
    ///
    /// using (var rd = guard.WaitForRundown())
    /// {
    ///     if (rd != null)
    ///     {
    ///         // This thread performs the teardown exclusively
    ///         // ... dispose resources, stop I/O, etc.
    ///     }
    ///     // If rd is null, another thread is handling teardown; this call blocks until completion.
    /// }
    /// ]]></code>
    /// </example>
    public IDisposable? WaitForRundown()
    {
        lock (_lock)
        {
            // If already completed, nothing to do.
            if (_rundownCompleted)
                return null;

            // If another thread is handling rundown, wait until it completes, then return null.
            if (_rundownStarted)
            {
                while (!_rundownCompleted)
                {
                    _ = Monitor.Wait(_lock);
                }
                return null;
            }

            // Become the rundown owner.
            _rundownStarted = true;
            _isShuttingDown = true;

            // Wait for all shared and exclusive holders to release.
            while (_activeCount > 0 || _exclusiveHeld)
            {
                _ = Monitor.Wait(_lock);
            }

            // Return a token; disposing it marks rundown complete and wakes other waiters.
            return new RundownToken(this);
        }
    }

    /// <summary>
    /// Releases a shared acquisition and wakes waiters.
    /// </summary>
    public void ReleaseShared()
    {
        lock (_lock)
        {
            if (_activeCount <= 0)
            {
                throw new InvalidOperationException("No shared guards held");
            }
            _activeCount--;
            // Wake up threads that might be waiting for capacity or for all shared to drain.
            Monitor.PulseAll(_lock);
        }
    }

    /// <summary>
    /// Releases an exclusive acquisition and wakes waiters.
    /// </summary>
    public void ReleaseExclusive()
    {
        lock (_lock)
        {
            if (_exclusiveHeld == false)
            {
                throw new InvalidOperationException("No exclusive guard held");
            }

            _exclusiveHeld = false;
            // Wake up threads waiting for exclusivity or rundown progress.
            Monitor.PulseAll(_lock);
        }
    }

    /// <summary>
    /// Token returned by <see cref="AcquireSharedToken"/>; disposing it releases the shared hold.
    /// </summary>
    private sealed class ProtectionToken : IDisposable
    {
        private readonly RundownGuard _owner;

        internal ProtectionToken(RundownGuard owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            _owner.ReleaseShared();
        }
    }

    /// <summary>
    /// Token returned by <see cref="AcquireExclusiveToken"/>; disposing it releases the exclusive hold.
    /// </summary>
    private sealed class ExclusiveToken : IDisposable
    {
        private readonly RundownGuard _owner;

        internal ExclusiveToken(RundownGuard owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            _owner.ReleaseExclusive();
        }
    }

    /// <summary>
    /// Token returned by <see cref="WaitForRundown"/> when this caller owns the rundown.
    /// Disposing it marks rundown as completed and wakes waiters.
    /// </summary>
    private sealed class RundownToken : IDisposable
    {
        private readonly RundownGuard _owner;

        internal RundownToken(RundownGuard owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            _owner.RundownComplete();
        }
    }
}
