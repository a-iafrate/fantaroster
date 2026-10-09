using System;

namespace Roster.Application.Participants.Queries;

public sealed record GetParticipantProfileQuery(
    Guid GameId,
    Guid ParticipantId
);

public sealed record ParticipantProfileDto(
    Guid Id,
    Guid GameId,
    string Nickname,
    int? Rank,
    decimal TotalScore
);
