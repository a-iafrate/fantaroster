using System;
using Roster.Domain.Participants;
using Shouldly;
using Xunit;

namespace Roster.Domain.Tests.Participants;

public class LineupTests
{
    [Fact]
    public void UpdatePicks_WhenLocked_ShouldThrowInvalidOperationException()
    {
        var lineup = new Lineup(Guid.NewGuid(), new[] { Guid.NewGuid() }, null, DateTimeOffset.UtcNow);
        lineup.Lock();

        Action act = () => lineup.UpdatePicks(new[] { Guid.NewGuid() }, null, DateTimeOffset.UtcNow);

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void UpdatePicks_WhenNotLocked_ShouldUpdateProperties()
    {
        var lineup = new Lineup(Guid.NewGuid(), new[] { Guid.NewGuid() }, null, DateTimeOffset.UtcNow);
        var newPicks = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var newCaptain = newPicks[0];
        var newTime = DateTimeOffset.UtcNow;

        lineup.UpdatePicks(newPicks, newCaptain, newTime);

        lineup.PickedElementIds.ShouldBe(newPicks);
        lineup.CaptainElementId.ShouldBe(newCaptain);
        lineup.SubmittedAt.ShouldBe(newTime);
    }
}
