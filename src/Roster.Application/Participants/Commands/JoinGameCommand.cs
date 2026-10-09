using System;

namespace Roster.Application.Participants.Commands;

public sealed record JoinGameCommand(
    Guid GameId,
    string Nickname
);
