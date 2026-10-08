using System;

namespace Roster.Domain.Scores;

public sealed class SponsorBonus
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string SponsorName { get; private set; }
    public int Points { get; private set; }
    public string QrToken { get; private set; }
    public int ScanCount { get; private set; }

    private SponsorBonus()
    {
        SponsorName = string.Empty;
        QrToken = string.Empty;
    }

    public SponsorBonus(Guid id, Guid gameId, string sponsorName, int points, string qrToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sponsorName);
        ArgumentException.ThrowIfNullOrWhiteSpace(qrToken);
        if (points == 0) throw new ArgumentException("Points must be non-zero.");

        Id = id;
        GameId = gameId;
        SponsorName = sponsorName;
        Points = points;
        QrToken = qrToken;
        ScanCount = 0;
    }

    public void IncrementScan()
    {
        ScanCount++;
    }
}
