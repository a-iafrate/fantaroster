using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roster.Domain.Games;
using Roster.Infrastructure.Data;
using Roster.Infrastructure.Data.Repositories;
using Xunit;
using Shouldly;

namespace Roster.IntegrationTests;

public sealed class GameRepositoryTests : IAsyncLifetime
{
    private RosterDbContext _dbContext = null!;
    private GameRepository _repository = null!;
    private UnitOfWork _unitOfWork = null!;

    public async Task InitializeAsync()
    {
        _dbContext = IntegrationDatabase.CreateContext();

        _repository = new GameRepository(_dbContext);
        _unitOfWork = new UnitOfWork(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task Add_And_GetById_ShouldPersistAndRetrieveGame()
    {
        // Arrange
        var id = Guid.NewGuid();
        var game = new Game(id, $"test-game-{id:N}", "Test Game", "generic", "brand", "en-US", 5, true, 1.5m, id.ToString("N")[..8].ToUpperInvariant(), DateTimeOffset.UtcNow);

        // Act
        _repository.Add(game);
        await _unitOfWork.SaveChangesAsync();

        var retrieved = await _repository.GetByIdAsync(id);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(id);
        retrieved.Slug.ShouldBe($"test-game-{id:N}");
        retrieved.Name.ShouldBe("Test Game");
    }
}
