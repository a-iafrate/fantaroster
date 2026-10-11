using Microsoft.AspNetCore.Http;
using Roster.Web.Security;
using Shouldly;
using Xunit;

namespace Roster.Web.Tests.Components;

public sealed class RateLimitingTests
{
    private static readonly RateLimitOptions Options = new();

    [Theory]
    [InlineData("POST", "/login", "login")]
    [InlineData("GET", "/auth/verify", "magic-link-verify")]
    [InlineData("GET", "/api/join-codes/ABCDE", "join-code")]
    [InlineData("POST", "/api/games/11111111-1111-1111-1111-111111111111/participant/join", "join")]
    [InlineData("POST", "/api/games/11111111-1111-1111-1111-111111111111/score-entries", "score-write")]
    [InlineData("POST", "/api/games/11111111-1111-1111-1111-111111111111/score-entries/22222222-2222-2222-2222-222222222222/void", "score-write")]
    [InlineData("GET", "/api/games/11111111-1111-1111-1111-111111111111/referee/me", "referee-check")]
    [InlineData("GET", "/consent/sometoken", "consent")]
    public void Classify_picks_the_rule_for_requests_that_guard_secrets(string method, string path, string policy) =>
        RateLimiting.Classify(method, path, Options)!.Value.Policy.ShouldBe(policy);

    [Theory]
    [InlineData("GET", "/login")]
    [InlineData("GET", "/")]
    [InlineData("GET", "/api/games/11111111-1111-1111-1111-111111111111/leaderboard")]
    [InlineData("GET", "/api/games/11111111-1111-1111-1111-111111111111/score-entries")]
    [InlineData("GET", "/games/11111111-1111-1111-1111-111111111111/big-screen")]
    [InlineData("GET", "/_framework/blazor.web.js")]
    public void Classify_leaves_everything_else_unlimited(string method, string path) =>
        RateLimiting.Classify(method, path, Options).ShouldBeNull();

    [Fact]
    public void A_limited_rule_rejects_the_request_after_its_permits_and_keeps_addresses_apart()
    {
        var options = new RateLimitOptions { JoinCodeLookupPerMinute = 3 };
        using var limiter = RateLimiting.Create(options);

        HttpContext From(string address, string path = "/api/join-codes/ABCDE")
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = path;
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(address);
            return context;
        }

        var first = From("10.0.0.1");
        for (var i = 0; i < 3; i++)
        {
            using var lease = limiter.AttemptAcquire(first);
            lease.IsAcquired.ShouldBeTrue();
        }

        limiter.AttemptAcquire(first).IsAcquired.ShouldBeFalse();
        limiter.AttemptAcquire(From("10.0.0.2")).IsAcquired.ShouldBeTrue();
        limiter.AttemptAcquire(From("10.0.0.1", "/")).IsAcquired.ShouldBeTrue();
    }

    [Fact]
    public void Referee_writes_are_limited_per_token_not_per_address()
    {
        var options = new RateLimitOptions { ScoreWritesPerMinute = 2 };
        using var limiter = RateLimiting.Create(options);

        HttpContext Write(string token)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/api/games/11111111-1111-1111-1111-111111111111/score-entries";
            context.Request.Headers.Authorization = $"Bearer {token}";
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.9");
            return context;
        }

        for (var i = 0; i < 2; i++)
        {
            using var lease = limiter.AttemptAcquire(Write("referee-a"));
            lease.IsAcquired.ShouldBeTrue();
        }

        limiter.AttemptAcquire(Write("referee-a")).IsAcquired.ShouldBeFalse();
        limiter.AttemptAcquire(Write("referee-b")).IsAcquired.ShouldBeTrue();
    }
}
