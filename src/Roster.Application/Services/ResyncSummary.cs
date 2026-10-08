namespace Roster.Application.Services;

public sealed record ResyncSummary(
    int AddedCount,
    int UpdatedCount,
    int MarkedMissingCount,
    IReadOnlyList<string> Warnings);
