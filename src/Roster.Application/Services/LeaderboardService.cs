using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Games.Queries;
using Roster.Application.Ports.Data;
using Roster.Domain.Scoring;

namespace Roster.Application.Services;

public sealed class LeaderboardService
{
    private readonly IGameRepository _gameRepository;
    private readonly IScoreEntryRepository _scoreEntryRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IElementRepository _elementRepository;

    public LeaderboardService(
        IGameRepository gameRepository,
        IScoreEntryRepository scoreEntryRepository,
        IParticipantRepository participantRepository,
        IElementRepository elementRepository)
    {
        _gameRepository = gameRepository;
        _scoreEntryRepository = scoreEntryRepository;
        _participantRepository = participantRepository;
        _elementRepository = elementRepository;
    }

    public async Task<LeaderboardResponseDto> HandleAsync(GetLeaderboardQuery query, CancellationToken cancellationToken)
    {
        var game = await _gameRepository.GetByIdAsync(query.GameId, cancellationToken);
        if (game == null)
            throw new ArgumentException($"Game {query.GameId} not found");

        var entries = await _scoreEntryRepository.GetValidEntriesByGameIdAsync(query.GameId, cancellationToken);
        var lineups = await _participantRepository.GetLineupsByGameIdAsync(query.GameId, cancellationToken);
        var participants = await _participantRepository.GetParticipantsByGameIdAsync(query.GameId, cancellationToken);
        var elements = await _elementRepository.GetByGameIdAsync(query.GameId, cancellationToken);

        var elementDict = elements.ToDictionary(e => e.Id);
        var participantDict = participants.ToDictionary(p => p.Id);

        // Calculate raw leaderboard
        var leaderboard = ScoringEngine.CalculateLeaderboard(game, entries, lineups);

        // Map to DTOs
        var dtos = new List<LeaderboardDto>();
        foreach (var pScore in leaderboard.Participants)
        {
            if (!participantDict.TryGetValue(pScore.ParticipantId, out var participant))
                continue;

            var lineup = lineups.FirstOrDefault(l => l.ParticipantId == pScore.ParticipantId);
            var elementScoreDtos = new List<ElementScoreDto>();

            if (lineup != null)
            {
                foreach (var elId in lineup.PickedElementIds)
                {
                    if (!elementDict.TryGetValue(elId, out var element))
                        continue;

                    var elScore = leaderboard.Elements.FirstOrDefault(e => e.ElementId == elId);
                    int scoreValue = elScore?.Score ?? 0;

                    bool isCaptain = game.CaptainEnabled && elId == lineup.CaptainElementId;
                    if (isCaptain)
                    {
                        scoreValue = (int)Math.Round(scoreValue * game.CaptainMultiplier, MidpointRounding.AwayFromZero);
                    }

                    elementScoreDtos.Add(new ElementScoreDto(elId, element.Name, scoreValue, isCaptain));
                }
            }

            dtos.Add(new LeaderboardDto(
                Rank: pScore.Rank,
                ParticipantId: pScore.ParticipantId,
                Nickname: participant.Nickname,
                TotalScore: pScore.Score,
                ElementScores: elementScoreDtos
            ));
        }

        var topPositions = dtos.OrderBy(d => d.Rank).Take(10).ToList();
        var myPosition = dtos.FirstOrDefault(d => d.ParticipantId == query.CallerParticipantId);

        return new LeaderboardResponseDto(myPosition, topPositions);
    }
}
