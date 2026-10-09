using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports.Data;
using Roster.Domain.Participants;

namespace Roster.Infrastructure.Data.Repositories;

internal sealed class ParticipantRepository : IParticipantRepository
{
    private readonly RosterDbContext _context;

    public ParticipantRepository(RosterDbContext context)
    {
        _context = context;
    }

    public async Task<Participant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Participants.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Participant?> GetByNicknameAsync(Guid gameId, string nickname, CancellationToken cancellationToken = default)
    {
        return await _context.Participants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GameId == gameId && p.Nickname == nickname, cancellationToken);
    }

    public async Task<Lineup?> GetLineupByParticipantIdAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        return await _context.Lineups.FirstOrDefaultAsync(l => l.ParticipantId == participantId, cancellationToken);
    }

    public async Task<List<Lineup>> GetLineupsByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var participantIds = await _context.Participants
            .AsNoTracking()
            .Where(p => p.GameId == gameId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (participantIds.Count == 0)
            return new List<Lineup>();

        return await _context.Lineups
            .AsNoTracking()
            .Where(l => participantIds.Contains(l.ParticipantId))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Participant>> GetParticipantsByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _context.Participants
            .AsNoTracking()
            .Where(p => p.GameId == gameId)
            .ToListAsync(cancellationToken);
    }

    public void Add(Participant participant)
    {
        _context.Participants.Add(participant);
    }

    public void Update(Participant participant)
    {
        _context.Participants.Update(participant);
    }

    public void AddLineup(Lineup lineup)
    {
        _context.Lineups.Add(lineup);
    }

    public void UpdateLineup(Lineup lineup)
    {
        _context.Lineups.Update(lineup);
    }
}
