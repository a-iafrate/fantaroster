using System;
using System.Collections.Generic;

namespace Roster.Application.Participants.Queries;

public sealed record GetParticipantLineupQuery(Guid ParticipantId, Guid GameId);

public sealed record ParticipantLineupDto(
    Guid ParticipantId,
    IReadOnlyList<Guid> PickedElementIds,
    Guid? CaptainElementId,
    DateTimeOffset SubmittedAt,
    bool IsLocked);
