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

public enum AdminGameSort
{
    Newest,
    Oldest,
    MostPlayers,
    Name,
}

/// <summary>Which games to list: a search on the name or on the organizer address, filters, an order and a page.</summary>
public sealed record AdminGameQuery(
    string? Search = null,
    GameState? State = null,
    bool CustomLimitOnly = false,
    AdminGameSort Sort = AdminGameSort.Newest,
    int Page = 1,
    int PageSize = AdminGameQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
}

/// <summary>One page of the games that match a query, with the total so the list can be paged.</summary>
public sealed record AdminGamePage(IReadOnlyList<AdminGame> Items, int Total, int Page, int PageSize)
{
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}

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
