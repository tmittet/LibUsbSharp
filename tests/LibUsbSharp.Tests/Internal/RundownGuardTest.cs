using FluentAssertions;
using LibUsbSharp.Internal;
using Xunit;

namespace LibUsbSharp.Tests.Internal;

public class RundownGuardTest : RundownGuard
{
    [Fact]
    public void AcquireShared_returns_immediately_when_no_other_guards_are_held()
    {
        var act = () => AcquireSharedToken(TimeSpan.FromMilliseconds(1));
        act.Should().NotThrow();
        ReleaseShared();
    }

    [Fact]
    public void AcquireShared_waits_for_release_when_ExclusiveGuard_is_held()
    {
        var exclusive = AcquireExclusiveToken()!;
        var act = () => AcquireSharedToken(TimeSpan.FromMilliseconds(1));
        act.Should().Throw<TimeoutException>().WithMessage("The operation has timed out.");
        exclusive.Dispose();
        act.Should().NotThrow();
        ReleaseShared();
    }

    [Fact]
    public void AcquireShared_throws_TimeoutException_when_wait_timeout_is_reached()
    {
        var exclusive = AcquireExclusiveToken()!;
        var act = () => AcquireSharedToken(TimeSpan.FromMilliseconds(1));
        act.Should().Throw<TimeoutException>().WithMessage("The operation has timed out.");
        exclusive.Dispose();
    }

    [Fact]
    public void AcquireShared_returns_null_when_rundown_is_triggered()
    {
        TriggerRundown();
        AcquireSharedToken(TimeSpan.FromMilliseconds(1)).Should().BeNull();
    }

    [Fact]
    public void Calling_ReleaseShared_when_no_guard_is_aquired_should_throw()
    {
        var act = () => ReleaseShared();
        act.Should().Throw<InvalidOperationException>().WithMessage("No shared guards held.");
    }

    [Fact]
    public void AcquireExclusive_returns_immediately_when_no_other_guards_are_held()
    {
        var act = () => AcquireExclusiveToken(TimeSpan.FromMilliseconds(1));
        act.Should().NotThrow();
        ReleaseExclusive();
    }

    [Fact]
    public void AcquireExclusive_waits_for_all_other_guards_to_release()
    {
        var exclusive = AcquireExclusiveToken()!;
        var act = () => AcquireExclusiveToken(TimeSpan.FromMilliseconds(1));
        act.Should().Throw<TimeoutException>().WithMessage("The operation has timed out.");
        exclusive.Dispose();
        act.Should().NotThrow();
        ReleaseExclusive();
    }

    [Fact]
    public void AcquireExclusive_throws_TimeoutException_when_wait_timeout_is_reached()
    {
        var exclusive = AcquireExclusiveToken()!;
        var act = () => AcquireExclusiveToken(TimeSpan.FromMilliseconds(1));
        act.Should().Throw<TimeoutException>().WithMessage("The operation has timed out.");
        exclusive.Dispose();
    }

    [Fact]
    public void AcquireExclusive_returns_null_when_rundown_is_triggered()
    {
        TriggerRundown();
        AcquireExclusiveToken(TimeSpan.FromMilliseconds(1)).Should().BeNull();
    }

    [Fact]
    public void Calling_ReleaseExclusive_when_no_guard_is_aquired_should_throw()
    {
        var act = () => ReleaseExclusive();
        act.Should().Throw<InvalidOperationException>().WithMessage("No exclusive guards held.");
    }

    [Fact]
    public void WaitForRundown_triggers_rundown()
    {
        var exclusive = AcquireExclusiveToken()!;
        var worker = new Thread(new ThreadStart(() => WaitForRundown()));
        worker.Start();
        worker.Join(1);
        exclusive.Dispose();
        AcquireExclusiveToken(TimeSpan.FromMilliseconds(1)).Should().BeNull();
        worker.Join();
    }

    [Fact]
    public void WaitForRundown_waits_for_all_guards_to_release()
    {
        var exclusive = AcquireExclusiveToken()!;
        var worker1 = new Thread(
            new ThreadStart(() =>
            {
                AcquireSharedToken();
                ReleaseShared();
            })
        );
        worker1.Start();
 
        var worker2 = new Thread(new ThreadStart(() => WaitForRundown()));
        worker2.Start();
        worker1.Join(1);
        worker2.Join(1);

        exclusive.Dispose();
        AcquireExclusiveToken(TimeSpan.FromMilliseconds(1)).Should().BeNull();
        worker1.Join();
        worker2.Join();
    }
}
