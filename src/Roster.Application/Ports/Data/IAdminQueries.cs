using Roster.Application.Admin;

namespace Roster.Application.Ports.Data;

/// <summary>Read-only aggregate queries for the internal administration dashboard.</summary>
public interface IAdminQueries
{
    Task<AdminStats> GetStatsAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>One page of the games that match the query, and how many match in all.</summary>
    Task<(List<AdminGameRow> Rows, int Total)> SearchGamesAsync(AdminGameQuery query, CancellationToken cancellationToken = default);

    /// <summary>Every game with its organizer and participant count, newest first.</summary>
    Task<List<AdminGameRow>> GetGamesAsync(CancellationToken cancellationToken = default);
}
