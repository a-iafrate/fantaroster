namespace Roster.Domain.Organizers;

/// <summary>
/// An organizer request to be recognised as a community (a user group, a club, a non-profit event series), which
/// unlocks the community participant limit. An administrator approves or rejects it.
/// </summary>
public sealed class CommunityRequest
{
    public Guid Id { get; private set; }

    /// <summary>The identity user id of the organizer.</summary>
    public string OrganizerId { get; private set; }

    /// <summary>The address of the organizer when the request was made, so the decision can be sent even if it changes.</summary>
    public string OrganizerEmail { get; private set; }

    public string Description { get; private set; }

    public string? Link { get; private set; }

    public CommunityRequestStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public string? DecidedBy { get; private set; }

    /// <summary>The note of the administrator, sent to the organizer with the decision.</summary>
    public string? DecisionNote { get; private set; }

    private CommunityRequest()
    {
        OrganizerId = string.Empty;
        OrganizerEmail = string.Empty;
        Description = string.Empty;
    }

    public CommunityRequest(Guid id, string organizerId, string organizerEmail, string description, string? link, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Id = id;
        OrganizerId = organizerId;
        OrganizerEmail = organizerEmail;
        Description = description.Trim();
        Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim();
        Status = CommunityRequestStatus.Pending;
        CreatedAt = createdAt;
    }

    public void Approve(string decidedBy, string? note, DateTimeOffset when) => Decide(CommunityRequestStatus.Approved, decidedBy, note, when);

    public void Reject(string decidedBy, string? note, DateTimeOffset when) => Decide(CommunityRequestStatus.Rejected, decidedBy, note, when);

    private void Decide(CommunityRequestStatus status, string decidedBy, string? note, DateTimeOffset when)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decidedBy);
        if (Status != CommunityRequestStatus.Pending)
        {
            throw new InvalidOperationException("The request has already been decided.");
        }

        Status = status;
        DecidedBy = decidedBy;
        DecisionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        DecidedAt = when;
    }
}
