using System;
using System.Collections.Generic;

namespace Roster.Application.Games.Queries;

public sealed record GetActivityFeedQuery(Guid GameId);

public sealed record ActivityFeedItemDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    string? ElementName,
    string? ParticipantNickname,
    string RuleLabel,
    int Points,
    string Status,
    string CreatedBy
);
