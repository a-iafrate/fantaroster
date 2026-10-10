using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Ports.Data;
using Roster.Application.Ports.Notifications;
using Roster.Application.Scores.Commands;
using Roster.Application.Scores.Queries;
using Roster.Domain.Games;
using Roster.Domain.Scores;

namespace Roster.Application.Services;

public sealed class ScoreManagementService
{
    private readonly IGameRepository _gameRepository;
    private readonly IScoreEntryRepository _scoreEntryRepository;
    private readonly IRuleRepository _ruleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGameNotificationService _notificationService;
    private readonly LeaderboardService _leaderboardService;
    private readonly TimeProvider _timeProvider;

    public ScoreManagementService(
        IGameRepository gameRepository,
        IScoreEntryRepository scoreEntryRepository,
        IRuleRepository ruleRepository,
        IUnitOfWork unitOfWork,
        IGameNotificationService notificationService,
        LeaderboardService leaderboardService,
        TimeProvider timeProvider)
    {
        _gameRepository = gameRepository;
        _scoreEntryRepository = scoreEntryRepository;
        _ruleRepository = ruleRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _leaderboardService = leaderboardService;
        _timeProvider = timeProvider;
    }

    public async Task<Guid?> AssignScoreAsync(AssignScoreCommand command, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(command.GameId, cancellationToken);
        if (game == null)
            throw new ArgumentException($"Game {command.GameId} not found.");

        if (game.State != GameState.Live)
            throw new InvalidOperationException("Scores can only be assigned when the game is Live.");

        if (await _scoreEntryRepository.ExistsByIdempotencyKeyAsync(command.GameId, command.IdempotencyKey, cancellationToken))
            return null; // Idempotent success, but we don't have the ID handy easily unless we return it. Returning null to indicate it was already processed.

        var rule = await _ruleRepository.GetByIdAsync(command.RuleId, cancellationToken);
        if (rule == null || rule.GameId != command.GameId)
            throw new ArgumentException($"Rule {command.RuleId} not found for this game.");

        var entry = new ScoreEntry(
            Guid.NewGuid(),
            command.GameId,
            command.ElementId,
            command.ParticipantId,
            command.RuleId,
            rule.Points,
            ScoreSource.Referee,
            command.RefereeName,
            _timeProvider.GetUtcNow(),
            command.IdempotencyKey
        );

        _scoreEntryRepository.Add(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyScoreEntryAddedAsync(command.GameId, entry.Id, cancellationToken);
        await _leaderboardService.RecomputeAndBroadcastAsync(command.GameId, cancellationToken);

        return entry.Id;
    }

    public async Task VoidScoreAsync(VoidScoreCommand command, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(command.GameId, cancellationToken);
        if (game == null)
            throw new ArgumentException($"Game {command.GameId} not found.");

        if (game.State != GameState.Live)
            throw new InvalidOperationException("Scores can only be voided when the game is Live.");

        var entry = await _scoreEntryRepository.GetByIdAsync(command.ScoreEntryId, cancellationToken);
        if (entry == null || entry.GameId != command.GameId)
            throw new ArgumentException($"Score entry {command.ScoreEntryId} not found.");

        if (entry.Status == ScoreStatus.Voided)
            return; // Already voided

        entry.Void(command.VoidedBy, _timeProvider.GetUtcNow());
        _scoreEntryRepository.Update(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyScoreEntryVoidedAsync(command.GameId, entry.Id, cancellationToken);
        await _leaderboardService.RecomputeAndBroadcastAsync(command.GameId, cancellationToken);
    }

    public async Task<System.Collections.Generic.List<ScoreEntryDto>> GetRecentScoresAsync(GetRecentScoresQuery query, CancellationToken cancellationToken = default)
    {
        var entries = await _scoreEntryRepository.GetRecentEntriesByGameIdAsync(query.GameId, query.Count, cancellationToken);
        return entries.Select(e => new ScoreEntryDto(
            e.Id,
            e.GameId,
            e.ElementId,
            e.ParticipantId,
            e.RuleId,
            e.PointsSnapshot,
            e.Source.ToString(),
            e.CreatedBy,
            e.CreatedAt,
            e.Status.ToString(),
            e.VoidedBy,
            e.VoidedAt,
            e.Note
        )).ToList();
    }
}
