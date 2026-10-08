using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roster.Domain.Games;
using Roster.Infrastructure.Data;
using Roster.Infrastructure.Data.Repositories;
using Testcontainers.MsSql;
using Xunit;
using Shouldly;

namespace Roster.IntegrationTests;

public sealed class GameRepositoryTests : IAsyncLifetime
{
    private MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private RosterDbContext _dbContext = null!;
    private GameRepository _repository = null!;
    private UnitOfWork _unitOfWork = null!;

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();

        var options = new DbContextOptionsBuilder<RosterDbContext>()
            .UseSqlServer(_msSqlContainer.GetConnectionString())
            .Options;

        _dbContext = new RosterDbContext(options);
        await _dbContext.Database.MigrateAsync();

        _repository = new GameRepository(_dbContext);
        _unitOfWork = new UnitOfWork(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _msSqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task Add_And_GetById_ShouldPersistAndRetrieveGame()
    {
        // Arrange
        var id = Guid.NewGuid();
        var game = new Game(id, "test-game", "Test Game", "generic", "brand", "en-US", 5, true, 1.5m, "JOIN", DateTimeOffset.UtcNow);

        // Act
        _repository.Add(game);
        await _unitOfWork.SaveChangesAsync();

        var retrieved = await _repository.GetByIdAsync(id);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(id);
        retrieved.Slug.ShouldBe("test-game");
        retrieved.Name.ShouldBe("Test Game");
    }
}
