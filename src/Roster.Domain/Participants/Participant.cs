using System;

namespace Roster.Domain.Participants;

public sealed class Participant
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string Nickname { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private Participant()
    {
        Nickname = string.Empty;
        TokenHash = string.Empty;
    }

    public Participant(Guid id, Guid gameId, string nickname, string tokenHash, DateTimeOffset joinedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        Id = id;
        GameId = gameId;
        Nickname = nickname;
        TokenHash = tokenHash;
        JoinedAt = joinedAt;
    }
}
