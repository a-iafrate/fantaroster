using System;

namespace Roster.Domain.Scores;

public sealed class Report
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public Guid ParticipantId { get; private set; }
    public Guid ElementId { get; private set; }
    public Guid? SuggestedRuleId { get; private set; }
    public string? Text { get; private set; }
    public string? PhotoBlobName { get; private set; }
    public ReportStatus Status { get; private set; }
    public Guid? ResultingScoreEntryId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Report() { }

    public Report(Guid id, Guid gameId, Guid participantId, Guid elementId, Guid? suggestedRuleId, string? text, string? photoBlobName, DateTimeOffset createdAt)
    {
        Id = id;
        GameId = gameId;
        ParticipantId = participantId;
        ElementId = elementId;
        SuggestedRuleId = suggestedRuleId;
        Text = text;
        PhotoBlobName = photoBlobName;
        Status = ReportStatus.Pending;
        CreatedAt = createdAt;
    }

    public void Approve(Guid scoreEntryId)
    {
        if (Status != ReportStatus.Pending) throw new InvalidOperationException("Only pending reports can be approved.");
        Status = ReportStatus.Approved;
        ResultingScoreEntryId = scoreEntryId;
    }

    public void Reject()
    {
        if (Status != ReportStatus.Pending) throw new InvalidOperationException("Only pending reports can be rejected.");
        Status = ReportStatus.Rejected;
    }
}
