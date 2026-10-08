using System;

namespace Roster.Domain.Games;

public sealed class Game
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; }
    public string Name { get; private set; }
    public string DomainPackId { get; private set; }
    public string Brand { get; private set; }
    public string Culture { get; private set; }
    public GameState State { get; private set; }
    public int LineupSize { get; private set; }
    public bool CaptainEnabled { get; private set; }
    public decimal CaptainMultiplier { get; private set; }
    public string JoinCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public byte[] RowVersion { get; private set; }

    private Game()
    {
        Slug = string.Empty;
        Name = string.Empty;
        DomainPackId = string.Empty;
        Brand = string.Empty;
        Culture = string.Empty;
        JoinCode = string.Empty;
        RowVersion = Array.Empty<byte>();
    }

    public Game(Guid id, string slug, string name, string domainPackId, string brand, string culture, int lineupSize, bool captainEnabled, decimal captainMultiplier, string joinCode, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(joinCode);

        Id = id;
        Slug = slug;
        Name = name;
        DomainPackId = domainPackId;
        Brand = brand;
        Culture = culture;
        State = GameState.Draft;
        LineupSize = lineupSize;
        CaptainEnabled = captainEnabled;
        CaptainMultiplier = captainMultiplier;
        JoinCode = joinCode;
        CreatedAt = createdAt;
        RowVersion = Array.Empty<byte>();
    }

    public void Open()
    {
        if (State != GameState.Draft) throw new InvalidOperationException("Game must be in Draft state to be opened.");
        State = GameState.Open;
    }

    public void GoLive(DateTimeOffset startedAt)
    {
        if (State != GameState.Open) throw new InvalidOperationException("Game must be in Open state to go live.");
        State = GameState.Live;
        StartedAt = startedAt;
    }

    public void End(DateTimeOffset endedAt)
    {
        if (State != GameState.Live) throw new InvalidOperationException("Game must be in Live state to end.");
        State = GameState.Ended;
        EndedAt = endedAt;
    }

    public void Archive()
    {
        if (State != GameState.Ended) throw new InvalidOperationException("Game must be in Ended state to be archived.");
        State = GameState.Archived;
    }
}
