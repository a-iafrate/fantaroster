namespace Roster.Web.Client.Services;

public enum RefereeSessionStatus
{
    Loading,
    Ready,

    /// <summary>No token on this device, or the token is no longer valid.</summary>
    NeedsInvite,
    NotFound,
    Error,
}

public enum AssignStatus
{
    /// <summary>The server accepted the point.</summary>
    Sent,

    /// <summary>No connection: the point waits in the offline queue and is sent later.</summary>
    Queued,

    /// <summary>The server refused it (for example the game is not live).</summary>
    Rejected,

    /// <summary>The token is not valid anymore.</summary>
    NotAuthorized,
}

/// <summary>Outcome of assigning a point; <see cref="EntryId"/> or <see cref="PendingKey"/> identify it for undo.</summary>
public sealed record AssignResult(AssignStatus Status, Guid? EntryId = null, string? PendingKey = null);

public sealed record RefereeInfo(string DisplayName);

public sealed record RefereeEntry(
    Guid Id,
    Guid? ElementId,
    Guid? ParticipantId,
    Guid RuleId,
    int PointsSnapshot,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    string Status,
    string? Note);

public enum RecentPointStatus
{
    Pending,
    Sent,
    Voided,
}

/// <summary>A point as the referee sees it: waiting to be sent, sent, or voided.</summary>
public sealed record RecentPoint(
    Guid? EntryId,
    string? PendingKey,
    Guid? ElementId,
    string ElementName,
    string RuleLabel,
    int Points,
    RecentPointStatus Status,
    DateTimeOffset? CreatedAt);
