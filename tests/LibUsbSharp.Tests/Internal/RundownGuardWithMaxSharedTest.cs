using FluentAssertions;
using LibUsbSharp.Internal;
using Xunit;

namespace LibUsbSharp.Tests.Internal;

public class RundownGuardWithMaxSharedTest : RundownGuard
{
    public RundownGuardWithMaxSharedTest()
        : base(maxSharedCount: 2) { }

    [Fact]
    public void AcquireShared_returns_immediately_when_maxSharedCount_is_not_reached()
    {
        AcquireSharedToken();
        var act = () => AcquireSharedToken(TimeSpan.FromMilliseconds(1));
        act.Should().NotThrow();
        ReleaseShared();
        ReleaseShared();
    }

    [Fact]
    public void AcquireShared_waits_for_release_when_maxSharedCount_is_reached()
    {
        var shared = AcquireSharedToken()!;
        _ = AcquireSharedToken();
        var act = () => AcquireSharedToken(TimeSpan.FromMilliseconds(1));
        act.Should().Throw<TimeoutException>().WithMessage("The operation has timed out.");
        shared.Dispose();
        act.Should().NotThrow();
        ReleaseShared();
    }
}
