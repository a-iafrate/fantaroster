using System;
using System.Collections.Generic;

namespace Roster.Application.Games.Queries;

public sealed record GetLeaderboardQuery(Guid GameId, Guid CallerParticipantId);

public sealed record LeaderboardResponseDto(
    LeaderboardDto? MyPosition,
    IReadOnlyList<LeaderboardDto> TopPositions
);

public sealed record LeaderboardDto(
    int Rank,
    Guid ParticipantId,
    string Nickname,
    int TotalScore,
    IReadOnlyList<ElementScoreDto> ElementScores
);

public sealed record ElementScoreDto(
    Guid ElementId,
    string Name,
    int Score,
    bool IsCaptain
);
