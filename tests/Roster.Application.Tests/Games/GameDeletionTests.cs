using NSubstitute;
using Roster.Application.Ports.Data;
using Roster.Application.Services;
using Roster.Domain.Games;
using Shouldly;
using Xunit;

namespace Roster.Application.Tests.Games;

public class GameDeletionTests
{
    private readonly IGameRepository _gameRepository = Substitute.For<IGameRepository>();
    private readonly GameLifecycleService _service;

    public GameDeletionTests()
    {
        _service = new GameLifecycleService(_gameRepository, Substitute.For<IUnitOfWork>(), TimeProvider.System);
    }

    [Theory]
    [InlineData(GameState.Draft)]
    [InlineData(GameState.Live)]
    public async Task DeleteGameAsync_ExistingGame_DeletesIt(GameState state)
    {
        var game = new Game(Guid.NewGuid(), "slug", "Name", "pack", "brand", "en", 3, true, 2m, "CODE", DateTimeOffset.UnixEpoch);
        if (state == GameState.Live)
        {
            game.Open();
            game.GoLive(DateTimeOffset.UnixEpoch);
        }

        _gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);

        await _service.DeleteGameAsync(game.Id);

        await _gameRepository.Received(1).DeleteAsync(game.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteGameAsync_UnknownGame_Throws()
    {
        await Should.ThrowAsync<ArgumentException>(() => _service.DeleteGameAsync(Guid.NewGuid()));

        await _gameRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
