using System;

namespace Roster.Application.Participants.Commands;

public sealed record SaveLineupCommand(
    Guid ParticipantId,
    Guid GameId,
    Guid[] PickedElementIds,
    Guid? CaptainElementId);
