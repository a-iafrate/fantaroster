using System;
using System.Collections.Generic;

namespace Roster.Domain.Participants;

public sealed class Lineup
{
    public Guid ParticipantId { get; private set; }
    public Guid[] PickedElementIds { get; private set; }
    public Guid? CaptainElementId { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public bool IsLocked { get; private set; }

    private Lineup()
    {
        PickedElementIds = Array.Empty<Guid>();
    }

    public Lineup(Guid participantId, Guid[] pickedElementIds, Guid? captainElementId, DateTimeOffset submittedAt)
    {
        ParticipantId = participantId;
        PickedElementIds = pickedElementIds ?? Array.Empty<Guid>();
        CaptainElementId = captainElementId;
        SubmittedAt = submittedAt;
        IsLocked = false;
    }

    public void Lock()
    {
        IsLocked = true;
    }

    public void UpdatePicks(Guid[] pickedElementIds, Guid? captainElementId, DateTimeOffset submittedAt)
    {
        if (IsLocked) throw new InvalidOperationException("Cannot update a locked lineup.");

        PickedElementIds = pickedElementIds ?? Array.Empty<Guid>();
        CaptainElementId = captainElementId;
        SubmittedAt = submittedAt;
    }
}
