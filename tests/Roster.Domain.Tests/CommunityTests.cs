using Roster.Domain.Games;
using Roster.Domain.Organizers;
using Shouldly;
using Xunit;

namespace Roster.Domain.Tests;

public class CommunityTests
{
    private static Game NewGame() => new(Guid.NewGuid(), "slug", "Game", "pack", "brand", "en", 3, true, 2m, "CODE1", DateTimeOffset.UnixEpoch);

    [Fact]
    public void EffectiveParticipantLimit_Default_IsTheFreeLimit() =>
        NewGame().EffectiveParticipantLimit(50, 300, organizerIsCommunity: false).ShouldBe(50);

    [Fact]
    public void EffectiveParticipantLimit_CommunityOrganizer_GetsTheCommunityLimit() =>
        NewGame().EffectiveParticipantLimit(50, 300, organizerIsCommunity: true).ShouldBe(300);

    [Fact]
    public void EffectiveParticipantLimit_Override_WinsOverThePlan()
    {
        var game = NewGame();
        game.SetParticipantLimitOverride(1000, " Launch event ");

        game.EffectiveParticipantLimit(50, 300, organizerIsCommunity: true).ShouldBe(1000);
        game.EffectiveParticipantLimit(50, 300, organizerIsCommunity: false).ShouldBe(1000);
        game.LimitLabel.ShouldBe("Launch event");
    }

    [Fact]
    public void SetParticipantLimitOverride_Null_ClearsLimitAndLabel()
    {
        var game = NewGame();
        game.SetParticipantLimitOverride(500, "Launch");

        game.SetParticipantLimitOverride(null, "ignored");

        game.ParticipantLimitOverride.ShouldBeNull();
        game.LimitLabel.ShouldBeNull();
        game.EffectiveParticipantLimit(50, 300, false).ShouldBe(50);
    }

    [Fact]
    public void SetParticipantLimitOverride_BelowOne_ShouldThrow() =>
        Should.Throw<ArgumentOutOfRangeException>(() => NewGame().SetParticipantLimitOverride(0, null));

    [Fact]
    public void CommunityRequest_StartsPendingAndCanBeDecidedOnce()
    {
        var request = new CommunityRequest(Guid.NewGuid(), "org-1", "a@example.test", " A user group ", " https://example.test ", DateTimeOffset.UnixEpoch);

        request.Status.ShouldBe(CommunityRequestStatus.Pending);
        request.Description.ShouldBe("A user group");
        request.Link.ShouldBe("https://example.test");

        request.Approve("admin@example.test", "Welcome", DateTimeOffset.UnixEpoch.AddDays(1));

        request.Status.ShouldBe(CommunityRequestStatus.Approved);
        request.DecisionNote.ShouldBe("Welcome");
        Should.Throw<InvalidOperationException>(() => request.Reject("admin@example.test", null, DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void CommunityRequest_Reject_RecordsWhoAndWhen()
    {
        var request = new CommunityRequest(Guid.NewGuid(), "org-1", "a@example.test", "Group", null, DateTimeOffset.UnixEpoch);

        request.Reject("admin@example.test", "  ", DateTimeOffset.UnixEpoch.AddHours(2));

        request.Status.ShouldBe(CommunityRequestStatus.Rejected);
        request.DecidedBy.ShouldBe("admin@example.test");
        request.DecisionNote.ShouldBeNull();
        request.DecidedAt.ShouldBe(DateTimeOffset.UnixEpoch.AddHours(2));
    }
}
