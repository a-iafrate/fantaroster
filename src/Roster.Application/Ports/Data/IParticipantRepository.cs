using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roster.Domain.Participants;

namespace Roster.Application.Ports.Data;

/// <summary>
/// Provides data access operations for Participant and Lineup entities.
/// </summary>
public interface IParticipantRepository
{
    Task<Participant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Participant?> GetByNicknameAsync(Guid gameId, string nickname, CancellationToken cancellationToken = default);
    Task<Lineup?> GetLineupByParticipantIdAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<List<Lineup>> GetLineupsByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<List<Participant>> GetParticipantsByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    void Add(Participant participant);
    void Update(Participant participant);
    void AddLineup(Lineup lineup);
    void UpdateLineup(Lineup lineup);
}
