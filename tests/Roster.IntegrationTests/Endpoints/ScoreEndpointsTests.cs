using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace Roster.IntegrationTests.Endpoints;

public class ScoreEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ScoreEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(Skip = "Requires database/Docker, skipped for now")]
    public async Task GetRecentScores_ReturnsOk()
    {
        var gameId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/games/{gameId}/score-entries");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }
}
