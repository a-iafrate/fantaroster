using System;

namespace Roster.Application.Scores.Commands;

public sealed record VoidScoreCommand(
    Guid GameId,
    Guid ScoreEntryId,
    string VoidedBy,
    string? Note
);
