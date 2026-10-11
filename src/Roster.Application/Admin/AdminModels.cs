using Roster.Domain.Games;

namespace Roster.Application.Admin;

public sealed record DailyCount(DateOnly Day, int Count);

public sealed record AdminStats(
    int Organizers,
    int Games,
    IReadOnlyDictionary<GameState, int> GamesByState,
    int Participants,
    int ParticipantsLast7Days,
    int ScoreEntriesValid,
    int ScoreEntriesVoided,
    int PendingCommunityRequests,
    int ApprovedCommunities,
    IReadOnlyList<DailyCount> JoinsPerDay,
    IReadOnlyList<AdminGameRow> TopGames);

/// <summary>A game as the query sees it, before the plan limits are applied.</summary>
public sealed record AdminGameRow(
    Guid Id,
    string Name,
    string? OrganizerEmail,
    bool OrganizerIsCommunity,
    GameState State,
    int Participants,
    DateTimeOffset CreatedAt,
    int? LimitOverride,
    string? LimitLabel);

public sealed record AdminGame(
    Guid Id,
    string Name,
    string? OrganizerEmail,
    GameState State,
    int Participants,
    DateTimeOffset CreatedAt,
    int EffectiveLimit,
    int? LimitOverride,
    string? LimitLabel);
