using Roster.Application.Games.Queries;
using Roster.Application.Ports.Data;
using Roster.Domain.Elements;

namespace Roster.Application.Services;

/// <summary>Counts and latest activity for the organizer overview and control screens.</summary>
public sealed record GameOverviewDto(
    int Participants,
    int Elements,
    int ConsentRequired,
    int ConsentAccepted,
    int ConsentPending,
    int ConsentDeclined,
    int Rules,
    int Referees,
    int PointsAssigned,
    IReadOnlyList<ActivityFeedItemDto> LatestPoints);

public sealed class GameOverviewService
{
    private const int LatestPointsCount = 5;

    private readonly IParticipantRepository _participants;
    private readonly IElementRepository _elements;
    private readonly IRuleRepository _rules;
    private readonly IRefereeRepository _referees;
    private readonly IScoreEntryRepository _scoreEntries;
    private readonly ActivityFeedService _activityFeed;

    public GameOverviewService(
        IParticipantRepository participants,
        IElementRepository elements,
        IRuleRepository rules,
        IRefereeRepository referees,
        IScoreEntryRepository scoreEntries,
        ActivityFeedService activityFeed)
    {
        _participants = participants;
        _elements = elements;
        _rules = rules;
        _referees = referees;
        _scoreEntries = scoreEntries;
        _activityFeed = activityFeed;
    }

    public async Task<GameOverviewDto> GetAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var participants = await _participants.GetParticipantsByGameIdAsync(gameId, cancellationToken);
        var elements = await _elements.GetByGameIdAsync(gameId, cancellationToken);
        var rules = await _rules.GetByGameIdAsync(gameId, cancellationToken);
        var referees = await _referees.GetByGameIdAsync(gameId, cancellationToken);
        var validEntries = await _scoreEntries.GetValidEntriesByGameIdAsync(gameId, cancellationToken);
        var feed = await _activityFeed.HandleAsync(new GetActivityFeedQuery(gameId), cancellationToken);

        var needingConsent = elements.Where(e => e.ConsentStatus != ConsentStatus.NotRequired).ToList();

        return new GameOverviewDto(
            Participants: participants.Count,
            Elements: elements.Count,
            ConsentRequired: needingConsent.Count,
            ConsentAccepted: needingConsent.Count(e => e.ConsentStatus == ConsentStatus.Accepted),
            ConsentPending: needingConsent.Count(e => e.ConsentStatus == ConsentStatus.Pending),
            ConsentDeclined: needingConsent.Count(e => e.ConsentStatus == ConsentStatus.Declined),
            Rules: rules.Count,
            Referees: referees.Count,
            PointsAssigned: validEntries.Count,
            LatestPoints: feed.Take(LatestPointsCount).ToList());
    }
}
