using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Roster.Application.Ports.Security;
using Roster.Domain.Games;
using Roster.Infrastructure.Data;
using Roster.Web.Endpoints;

namespace Roster.IntegrationTests.Endpoints;

public class ParticipantEndpointsTests : IClassFixture<RosterWebFactory>
{
    private readonly RosterWebFactory _factory;

    public ParticipantEndpointsTests(RosterWebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>A game with a slug and a join code of its own: all tests share one database.</summary>
    private static Game NewGame()
    {
        var id = Guid.NewGuid();
        return new Game(id, $"slug-{id:N}", "My Game", "pack", "brand", "en", 5, true, 1.5m, id.ToString("N")[..8].ToUpperInvariant(), DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task JoinGame_WhenGameDoesNotExist_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var gameId = Guid.NewGuid();
        var request = new JoinGameRequest("Player1");

        // Act
        var response = await client.PostAsJsonAsync($"/api/games/{gameId}/participant/join", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task JoinGame_WhenGameIsOpen_ReturnsOkWithToken()
    {
        // Arrange
        var client = _factory.CreateClient();

        // We need to create a game first directly in the DB to test the success path
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RosterDbContext>();

        var game = NewGame();
        game.Open();
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();

        var request = new JoinGameRequest("Player1");

        // Act
        var response = await client.PostAsJsonAsync($"/api/games/{game.Id}/participant/join", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JoinResponse>();
        content.ShouldNotBeNull();
        content.Token.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetParticipantProfile_WithValidToken_ReturnsProfile()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RosterDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IParticipantTokenService>();

        var game = NewGame();
        game.Open();
        dbContext.Games.Add(game);

        var participantId = Guid.NewGuid();
        var (token, hash) = tokenService.GenerateToken(participantId, game.Id, "Player2");
        var participant = new Roster.Domain.Participants.Participant(participantId, game.Id, "Player2", hash, DateTimeOffset.UtcNow);
        dbContext.Participants.Add(participant);

        await dbContext.SaveChangesAsync();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/games/{game.Id}/participant/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<Roster.Application.Participants.Queries.ParticipantProfileDto>();
        profile.ShouldNotBeNull();
        profile.Nickname.ShouldBe("Player2");
        profile.Id.ShouldBe(participantId);
    }

    private sealed class JoinResponse
    {
        public string? Token { get; set; }
    }
}
