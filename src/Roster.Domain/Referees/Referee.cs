using System;

namespace Roster.Domain.Referees;

public sealed class Referee
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string DisplayName { get; private set; }
    public string InviteTokenHash { get; private set; }
    public string[] Scope { get; private set; }

    private Referee()
    {
        DisplayName = string.Empty;
        InviteTokenHash = string.Empty;
        Scope = Array.Empty<string>();
    }

    public Referee(Guid id, Guid gameId, string displayName, string inviteTokenHash, string[] scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(inviteTokenHash);

        Id = id;
        GameId = gameId;
        DisplayName = displayName;
        InviteTokenHash = inviteTokenHash;
        Scope = scope ?? Array.Empty<string>();
    }
}
