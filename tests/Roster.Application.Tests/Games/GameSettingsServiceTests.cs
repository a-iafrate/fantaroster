using NSubstitute;
using Roster.Application.Ports.Data;
using Roster.Application.Services;
using Roster.Domain.Games;
using Roster.Domain.Participants;
using Shouldly;
using Xunit;

namespace Roster.Application.Tests.Games;

public class GameSettingsServiceTests
{
    private readonly IGameRepository _gameRepository = Substitute.For<IGameRepository>();
    private readonly IParticipantRepository _participantRepository = Substitute.For<IParticipantRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GameSettingsService _service;
    private readonly Game _game = new(Guid.NewGuid(), "slug", "Old name", "pack", "brand", "en", 3, true, 2m, "CODE", DateTimeOffset.UnixEpoch);

    public GameSettingsServiceTests()
    {
        _service = new GameSettingsService(_gameRepository, _participantRepository, _unitOfWork);
        _gameRepository.GetByIdAsync(_game.Id, Arg.Any<CancellationToken>()).Returns(_game);
        _participantRepository.GetParticipantsByGameIdAsync(_game.Id, Arg.Any<CancellationToken>()).Returns([]);
    }

    [Fact]
    public async Task UpdateAsync_NoParticipants_ChangesNameAndLineupSize()
    {
        await _service.UpdateAsync(_game.Id, "  New name ", 5);

        _game.Name.ShouldBe("New name");
        _game.LineupSize.ShouldBe(5);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithParticipants_RenamesButKeepsLineupSize()
    {
        WithParticipant();

        await _service.UpdateAsync(_game.Id, "New name", 3);

        _game.Name.ShouldBe("New name");
        _game.LineupSize.ShouldBe(3);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithParticipantsAndNewLineupSize_Throws()
    {
        WithParticipant();

        await Should.ThrowAsync<InvalidOperationException>(() => _service.UpdateAsync(_game.Id, "New name", 5));

        _game.Name.ShouldBe("Old name");
        _game.LineupSize.ShouldBe(3);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void WithParticipant()
    {
        var participant = new Participant(Guid.NewGuid(), _game.Id, "nick", "hash", DateTimeOffset.UnixEpoch);
        _participantRepository.GetParticipantsByGameIdAsync(_game.Id, Arg.Any<CancellationToken>()).Returns([participant]);
    }

    [Fact]
    public async Task UpdateAsync_InvalidLineupSize_Throws()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _service.UpdateAsync(_game.Id, "New name", 0));
    }

    [Fact]
    public async Task CanEditAsync_WithParticipants_ReturnsFalse()
    {
        var participant = new Participant(Guid.NewGuid(), _game.Id, "nick", "hash", DateTimeOffset.UnixEpoch);
        _participantRepository.GetParticipantsByGameIdAsync(_game.Id, Arg.Any<CancellationToken>()).Returns([participant]);

        (await _service.CanEditAsync(_game.Id)).ShouldBeFalse();
    }
}
