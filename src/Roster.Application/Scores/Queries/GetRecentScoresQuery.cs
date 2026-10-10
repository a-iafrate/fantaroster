using System;

namespace Roster.Application.Scores.Queries;

public sealed record GetRecentScoresQuery(Guid GameId, int Count);

public sealed record ScoreEntryDto(
    Guid Id,
    Guid GameId,
    Guid? ElementId,
    Guid? ParticipantId,
    Guid RuleId,
    int PointsSnapshot,
    string Source,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    string Status,
    string? VoidedBy,
    DateTimeOffset? VoidedAt,
    string? Note
);
