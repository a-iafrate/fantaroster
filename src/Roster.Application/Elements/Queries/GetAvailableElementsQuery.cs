using System;

namespace Roster.Application.Elements.Queries;

public sealed record GetAvailableElementsQuery(Guid GameId);

public sealed record AvailableElementDto(
    Guid Id,
    string Name,
    string? Subtitle,
    string? Group,
    string? ImageUrl);
