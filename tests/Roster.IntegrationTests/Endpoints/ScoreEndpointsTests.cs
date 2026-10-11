using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace Roster.IntegrationTests.Endpoints;

public class ScoreEndpointsTests : IClassFixture<RosterWebFactory>
{
    private readonly HttpClient _client;

    public ScoreEndpointsTests(RosterWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRecentScores_ForAGameWithoutPoints_ReturnsAnEmptyList()
    {
        var gameId = Guid.NewGuid();
        await using (var db = IntegrationDatabase.CreateContext())
        {
            db.Games.Add(new Roster.Domain.Games.Game(gameId, $"slug-{gameId:N}", "Game", "generic", "brand", "en", 3, true, 2m, gameId.ToString("N")[..8], DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/games/{gameId}/score-entries");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadAsStringAsync()).ShouldBe("[]");
    }

    [Fact]
    public async Task AssignScore_WithoutRefereeToken_IsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{Guid.NewGuid()}/score-entries")
        {
            Content = JsonContent.Create(new { RuleId = Guid.NewGuid() }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized, await response.Content.ReadAsStringAsync());
    }
}
