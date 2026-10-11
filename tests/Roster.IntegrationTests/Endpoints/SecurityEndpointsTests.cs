using System.Net;
using System.Net.Http.Json;
using Roster.Web.Endpoints;
using Shouldly;
using Xunit;

namespace Roster.IntegrationTests.Endpoints;

public sealed class SecurityEndpointsTests : IClassFixture<RosterWebFactory>
{
    private readonly RosterWebFactory _factory;

    public SecurityEndpointsTests(RosterWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Join_code_lookups_are_rate_limited_per_address()
    {
        var client = _factory.CreateClient();
        var statuses = new List<HttpStatusCode>();

        // The test host allows three lookups per minute.
        for (var i = 0; i < 5; i++)
        {
            using var response = await client.GetAsync("/api/join-codes/ZZZZZ");
            statuses.Add(response.StatusCode);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                response.Headers.RetryAfter.ShouldNotBeNull();
            }
        }

        statuses.Take(3).ShouldAllBe(s => s == HttpStatusCode.NotFound);
        statuses.Skip(3).ShouldAllBe(s => s == HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task An_unknown_game_is_a_plain_404_for_API_clients()
    {
        var client = _factory.CreateClient();

        using var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("this nickname is far too long to be accepted by the edge validation of the join endpoint")]
    public async Task Join_rejects_nicknames_outside_the_allowed_length(string nickname)
    {
        var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync($"/api/games/{Guid.NewGuid()}/participant/join", new JoinGameRequest(nickname));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Nickname");
    }
}
