using System;

namespace Roster.Domain.Scores;

public sealed class ScoreEntry
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public Guid? ElementId { get; private set; }
    public Guid? ParticipantId { get; private set; }
    public Guid RuleId { get; private set; }
    public int PointsSnapshot { get; private set; }
    public ScoreSource Source { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string IdempotencyKey { get; private set; }
    public ScoreStatus Status { get; private set; }
    public string? VoidedBy { get; private set; }
    public DateTimeOffset? VoidedAt { get; private set; }
    public string? Note { get; private set; }

    private ScoreEntry()
    {
        CreatedBy = string.Empty;
        IdempotencyKey = string.Empty;
    }

    public ScoreEntry(Guid id, Guid gameId, Guid? elementId, Guid? participantId, Guid ruleId, int pointsSnapshot, ScoreSource source, string createdBy, DateTimeOffset createdAt, string idempotencyKey, string? note = null)
    {
        if (elementId == null && participantId == null) throw new ArgumentException("Either ElementId or ParticipantId must be provided.");
        if (pointsSnapshot == 0) throw new ArgumentException("Points snapshot must be non-zero.");
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        Id = id;
        GameId = gameId;
        ElementId = elementId;
        ParticipantId = participantId;
        RuleId = ruleId;
        PointsSnapshot = pointsSnapshot;
        Source = source;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        IdempotencyKey = idempotencyKey;
        Status = ScoreStatus.Valid;
        Note = note;
    }

    public void Void(string voidedBy, DateTimeOffset voidedAt)
    {
        if (Status == ScoreStatus.Voided) return;
        ArgumentException.ThrowIfNullOrWhiteSpace(voidedBy);

        Status = ScoreStatus.Voided;
        VoidedBy = voidedBy;
        VoidedAt = voidedAt;
    }
}
