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

    /// <summary>The organizer (identity user id) who owns the game; only that person can see and manage it.</summary>
    public string? OrganizerId { get; private set; }

    /// <summary>A participant limit set by an administrator for this game (for instance a launch event); it wins over the plan.</summary>
    public int? ParticipantLimitOverride { get; private set; }

    /// <summary>Why the limit was raised (shown to administrators only), e.g. "Launch event".</summary>
    public string? LimitLabel { get; private set; }

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

    public void AssignToOrganizer(string organizerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);
        if (OrganizerId is not null && OrganizerId != organizerId)
        {
            throw new InvalidOperationException("A game belongs to one organizer and cannot be handed over by accident.");
        }
        OrganizerId = organizerId;
    }

    /// <summary>Whether the given organizer owns the game. A game without an owner belongs to nobody.</summary>
    public bool IsOwnedBy(string? organizerId) =>
        !string.IsNullOrEmpty(organizerId) && OrganizerId is not null && string.Equals(OrganizerId, organizerId, StringComparison.Ordinal);

    /// <summary>Sets or clears (<c>null</c>) the administrator limit of participants for this game.</summary>
    public void SetParticipantLimitOverride(int? limit, string? label)
    {
        if (limit is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "The participant limit must be at least 1.");
        }

        ParticipantLimitOverride = limit;
        LimitLabel = limit is null || string.IsNullOrWhiteSpace(label) ? null : label.Trim();
    }

    /// <summary>
    /// The most participants the game accepts: the administrator limit if there is one, otherwise the community limit
    /// when the organizer is an approved community, otherwise the free limit.
    /// </summary>
    public int EffectiveParticipantLimit(int freeLimit, int communityLimit, bool organizerIsCommunity) =>
        ParticipantLimitOverride ?? (organizerIsCommunity ? Math.Max(freeLimit, communityLimit) : freeLimit);

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void ChangeLineupSize(int lineupSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(lineupSize, 1);
        LineupSize = lineupSize;
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
