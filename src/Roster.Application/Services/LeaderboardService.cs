using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Games.Queries;
using Roster.Application.Ports.Data;
using Roster.Application.Ports.Notifications;
using Roster.Domain.Scoring;

namespace Roster.Application.Services;

public sealed class LeaderboardService
{
    private readonly IGameRepository _gameRepository;
    private readonly IScoreEntryRepository _scoreEntryRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IElementRepository _elementRepository;
    private readonly LeaderboardStateStore _stateStore;
    private readonly IGameNotificationService _notificationService;

    public LeaderboardService(
        IGameRepository gameRepository,
        IScoreEntryRepository scoreEntryRepository,
        IParticipantRepository participantRepository,
        IElementRepository elementRepository,
        LeaderboardStateStore stateStore,
        IGameNotificationService notificationService)
    {
        _gameRepository = gameRepository;
        _scoreEntryRepository = scoreEntryRepository;
        _participantRepository = participantRepository;
        _elementRepository = elementRepository;
        _stateStore = stateStore;
        _notificationService = notificationService;
    }

    public async Task<LeaderboardResponseDto> HandleAsync(GetLeaderboardQuery query, CancellationToken cancellationToken)
    {
        var state = _stateStore.GetOrAddState(query.GameId);
        var cached = state.GetLeaderboard();

        if (cached == null)
        {
            cached = await RecomputeCoreAsync(query.GameId, cancellationToken);
            state.Update(cached);
        }

        var topPositions = cached.OrderBy(d => d.Rank).Take(10).ToList();
        var myPosition = cached.FirstOrDefault(d => d.ParticipantId == query.CallerParticipantId);

        return new LeaderboardResponseDto(myPosition, topPositions, cached.Count, state.Version);
    }

    public async Task RecomputeAndBroadcastAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var newLeaderboard = await RecomputeCoreAsync(gameId, cancellationToken);

        var state = _stateStore.GetOrAddState(gameId);
        state.Update(newLeaderboard);

        await _notificationService.NotifyLeaderboardUpdatedAsync(gameId, state.Version, cancellationToken);
    }

    private async Task<List<LeaderboardDto>> RecomputeCoreAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null)
            throw new ArgumentException($"Game {gameId} not found");

        var entries = await _scoreEntryRepository.GetValidEntriesByGameIdAsync(gameId, cancellationToken);
        var lineups = await _participantRepository.GetLineupsByGameIdAsync(gameId, cancellationToken);
        var participants = await _participantRepository.GetParticipantsByGameIdAsync(gameId, cancellationToken);
        var elements = await _elementRepository.GetByGameIdAsync(gameId, cancellationToken);

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

        return dtos;
    }
}
