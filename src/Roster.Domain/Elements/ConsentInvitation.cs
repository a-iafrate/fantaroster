using System;

namespace Roster.Domain.Elements;

public sealed class ConsentInvitation
{
    public Guid Id { get; private set; }
    public Guid ElementId { get; private set; }
    public string TokenHash { get; private set; }
    public string? Contact { get; private set; }
    public DateTimeOffset SentAt { get; private set; }
    public DateTimeOffset? AnsweredAt { get; private set; }

    private ConsentInvitation()
    {
        TokenHash = string.Empty;
    }

    public ConsentInvitation(Guid id, Guid elementId, string tokenHash, string? contact, DateTimeOffset sentAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        Id = id;
        ElementId = elementId;
        TokenHash = tokenHash;
        Contact = contact;
        SentAt = sentAt;
    }

    public void MarkAnswered(DateTimeOffset answeredAt)
    {
        AnsweredAt = answeredAt;
    }
}
