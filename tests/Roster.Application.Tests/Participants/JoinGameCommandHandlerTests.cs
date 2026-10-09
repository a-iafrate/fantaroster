using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;
using Roster.Application.Participants.Commands;
using Roster.Application.Ports.Data;
using Roster.Application.Ports.Security;
using Roster.Application.Services;
using Roster.Domain.Games;
using Roster.Domain.Participants;

namespace Roster.Application.Tests.Participants;

public class JoinGameCommandHandlerTests
{
    private readonly IGameRepository _gameRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IElementRepository _elementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IParticipantTokenService _tokenService;
    private readonly IProfanityFilter _profanityFilter;
    private readonly ParticipantManagementService _service;

    public JoinGameCommandHandlerTests()
    {
        _gameRepository = Substitute.For<IGameRepository>();
        _participantRepository = Substitute.For<IParticipantRepository>();
        _elementRepository = Substitute.For<IElementRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _tokenService = Substitute.For<IParticipantTokenService>();
        _profanityFilter = Substitute.For<IProfanityFilter>();

        _service = new ParticipantManagementService(
            _gameRepository,
            _participantRepository,
            _elementRepository,
            _unitOfWork,
            _tokenService,
            _profanityFilter,
            TimeProvider.System);
    }

    [Fact]
    public async Task JoinGameAsync_WhenGameNotFound_ThrowsArgumentException()
    {
        // Arrange
        var command = new JoinGameCommand(Guid.NewGuid(), "Player1");
        _gameRepository.GetByIdAsync(command.GameId, Arg.Any<CancellationToken>()).Returns((Game?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _service.JoinGameAsync(command));
    }

    [Fact]
    public async Task JoinGameAsync_WhenGameNotOpenOrLive_ThrowsInvalidOperationException()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = new Game(gameId, "slug", "My Game", "pack", "brand", "en", 5, true, 1.5m, "CODE", DateTimeOffset.UtcNow);
        // State is Draft by default

        var command = new JoinGameCommand(gameId, "Player1");
        _gameRepository.GetByIdAsync(command.GameId, Arg.Any<CancellationToken>()).Returns(game);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _service.JoinGameAsync(command));
    }

    [Fact]
    public async Task JoinGameAsync_WhenProfanityDetected_ThrowsArgumentException()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = new Game(gameId, "slug", "My Game", "pack", "brand", "en", 5, true, 1.5m, "CODE", DateTimeOffset.UtcNow);
        game.Open();

        var command = new JoinGameCommand(gameId, "BadWord");
        _gameRepository.GetByIdAsync(command.GameId, Arg.Any<CancellationToken>()).Returns(game);
        _profanityFilter.ContainsProfanity("BadWord").Returns(true);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _service.JoinGameAsync(command));
    }

    [Fact]
    public async Task JoinGameAsync_WhenNicknameTaken_ThrowsInvalidOperationException()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = new Game(gameId, "slug", "My Game", "pack", "brand", "en", 5, true, 1.5m, "CODE", DateTimeOffset.UtcNow);
        game.Open();

        var command = new JoinGameCommand(gameId, "Player1");
        _gameRepository.GetByIdAsync(command.GameId, Arg.Any<CancellationToken>()).Returns(game);

        var existing = new Participant(Guid.NewGuid(), gameId, "Player1", "hash", DateTimeOffset.UtcNow);
        _participantRepository.GetByNicknameAsync(command.GameId, command.Nickname, Arg.Any<CancellationToken>()).Returns(existing);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _service.JoinGameAsync(command));
    }

    [Fact]
    public async Task JoinGameAsync_WhenValid_ReturnsToken()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = new Game(gameId, "slug", "My Game", "pack", "brand", "en", 5, true, 1.5m, "CODE", DateTimeOffset.UtcNow);
        game.Open();

        var command = new JoinGameCommand(gameId, "Player1");
        _gameRepository.GetByIdAsync(command.GameId, Arg.Any<CancellationToken>()).Returns(game);
        _participantRepository.GetByNicknameAsync(command.GameId, command.Nickname, Arg.Any<CancellationToken>()).Returns((Participant?)null);
        _tokenService.GenerateToken(Arg.Any<Guid>(), command.GameId, command.Nickname).Returns(("raw-token", "token-hash"));

        // Act
        var token = await _service.JoinGameAsync(command);

        // Assert
        token.ShouldBe("raw-token");
        _participantRepository.Received(1).Add(Arg.Is<Participant>(p => p.Nickname == "Player1" && p.TokenHash == "token-hash"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
