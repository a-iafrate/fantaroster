namespace Roster.Web.Client.Services;

/// <summary>Public game information, with the words defined by the game's domain pack.</summary>
public sealed record GameInfo(
    string Name,
    int LineupSize,
    bool CaptainEnabled,
    decimal CaptainMultiplier,
    string State,
    GameTerms Terms)
{
    public bool IsBeforeStart => State is "Draft" or "Open";

    public bool IsLive => State == "Live";

    public bool IsOver => State is "Ended" or "Archived";
}

public sealed record GameTerms(string ElementSingular, string ElementPlural, string GroupLabel);

public sealed record ParticipantProfile(Guid Id, Guid GameId, string Nickname, int? Rank, decimal TotalScore);

public sealed record AvailableElement(Guid Id, string Name, string? Subtitle, string? Group, string? ImageUrl);

public sealed record ParticipantLineup(Guid ParticipantId, IReadOnlyList<Guid> PickedElementIds, Guid? CaptainElementId, bool IsLocked);

public sealed record LeaderboardResponse(
    LeaderboardEntry? MyPosition,
    IReadOnlyList<LeaderboardEntry> TopPositions,
    int TotalParticipants,
    long Version);

public sealed record LeaderboardEntry(int Rank, Guid ParticipantId, string Nickname, int TotalScore, IReadOnlyList<ElementScore> ElementScores);

public sealed record ElementScore(Guid ElementId, string Name, int Score, bool IsCaptain);

public sealed record FeedItem(
    Guid Id,
    DateTimeOffset CreatedAt,
    Guid? ElementId,
    string? ElementName,
    Guid? ParticipantId,
    string? ParticipantNickname,
    string RuleLabel,
    int Points,
    string Status,
    string CreatedBy)
{
    public bool IsVoided => Status == "Voided";
}

public sealed record GameRule(Guid Id, string Label, int Points, string? Category, string Target)
{
    public bool IsPersonal => Target == "Participant";
}

/// <summary>A new point that concerns the participant: one of their picks, or a personal bonus.</summary>
public sealed record PointNotification(FeedItem Item, int? Rank);

public enum ParticipantSessionStatus
{
    Loading,
    Ready,
    NeedsJoin,
    NotFound,
    Error,
}

public enum SaveLineupOutcome
{
    Saved,
    Rejected,
    NetworkError,
}
