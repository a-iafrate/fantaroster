using System;

namespace Roster.Application.Scores.Commands;

public sealed record AssignScoreCommand(
    Guid GameId,
    Guid? ElementId,
    Guid? ParticipantId,
    Guid RuleId,
    string RefereeName, // For tracking who assigned it
    string IdempotencyKey
);
