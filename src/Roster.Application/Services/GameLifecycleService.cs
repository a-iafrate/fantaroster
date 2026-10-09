using System;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Ports.Data;
using Roster.Domain.Games;

namespace Roster.Application.Services;

public sealed class GameLifecycleService
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public GameLifecycleService(
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task OpenGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null) throw new ArgumentException($"Game {gameId} not found.");

        game.Open();
        _gameRepository.Update(game);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task GoLiveAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null) throw new ArgumentException($"Game {gameId} not found.");

        game.GoLive(_timeProvider.GetUtcNow());
        _gameRepository.Update(game);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task EndGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null) throw new ArgumentException($"Game {gameId} not found.");

        game.End(_timeProvider.GetUtcNow());
        _gameRepository.Update(game);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null) throw new ArgumentException($"Game {gameId} not found.");

        game.Archive();
        _gameRepository.Update(game);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
