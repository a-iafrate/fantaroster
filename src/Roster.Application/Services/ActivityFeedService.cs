using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Games.Queries;
using Roster.Application.Ports.Data;
using Roster.Domain.Scores;

namespace Roster.Application.Services;

public sealed class ActivityFeedService
{
    private readonly IScoreEntryRepository _scoreEntryRepository;
    private readonly IElementRepository _elementRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IRuleRepository _ruleRepository;

    public ActivityFeedService(
        IScoreEntryRepository scoreEntryRepository,
        IElementRepository elementRepository,
        IParticipantRepository participantRepository,
        IRuleRepository ruleRepository)
    {
        _scoreEntryRepository = scoreEntryRepository;
        _elementRepository = elementRepository;
        _participantRepository = participantRepository;
        _ruleRepository = ruleRepository;
    }

    public async Task<List<ActivityFeedItemDto>> HandleAsync(GetActivityFeedQuery query, CancellationToken cancellationToken)
    {
        // 50 latest entries should be enough for an activity feed payload
        var entries = await _scoreEntryRepository.GetRecentEntriesByGameIdAsync(query.GameId, 50, cancellationToken);

        if (entries.Count == 0)
        {
            return new List<ActivityFeedItemDto>();
        }

        var elements = await _elementRepository.GetByGameIdAsync(query.GameId, cancellationToken);
        var participants = await _participantRepository.GetParticipantsByGameIdAsync(query.GameId, cancellationToken);
        var rules = await _ruleRepository.GetByGameIdAsync(query.GameId, cancellationToken);

        var elementDict = elements.ToDictionary(e => e.Id);
        var participantDict = participants.ToDictionary(p => p.Id);
        var ruleDict = rules.ToDictionary(r => r.Id);

        var result = new List<ActivityFeedItemDto>();

        foreach (var entry in entries)
        {
            string? elementName = null;
            if (entry.ElementId.HasValue && elementDict.TryGetValue(entry.ElementId.Value, out var element))
            {
                elementName = element.Name;
            }

            string? participantNickname = null;
            if (entry.ParticipantId.HasValue && participantDict.TryGetValue(entry.ParticipantId.Value, out var participant))
            {
                participantNickname = participant.Nickname;
            }

            string ruleLabel = "Unknown Rule";
            if (ruleDict.TryGetValue(entry.RuleId, out var rule))
            {
                ruleLabel = rule.Label;
            }

            result.Add(new ActivityFeedItemDto(
                Id: entry.Id,
                CreatedAt: entry.CreatedAt,
                ElementId: entry.ElementId,
                ElementName: elementName,
                ParticipantId: entry.ParticipantId,
                ParticipantNickname: participantNickname,
                RuleLabel: ruleLabel,
                Points: entry.PointsSnapshot,
                Status: entry.Status.ToString(),
                CreatedBy: entry.CreatedBy
            ));
        }

        return result;
    }
}
