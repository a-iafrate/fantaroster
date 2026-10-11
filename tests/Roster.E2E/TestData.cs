using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Services;
using Roster.Domain.Elements;
using Roster.Domain.Games;
using Roster.Domain.Participants;
using Roster.Domain.Referees;
using Roster.Infrastructure.Data;

namespace Roster.E2E;

public enum TestGameState
{
    Open,
    Live,
    Ended,
}

/// <summary>A context to run the same journey against: people (a conference) or teams (a tournament).</summary>
public sealed record TestScenario(string DomainPackId, string GameName, IReadOnlyList<(string Name, string? Subtitle, string Group)> Elements, IReadOnlyList<(string Label, int Points)> Rules)
{
    public static TestScenario Conference { get; } = new(
        "tech-conference",
        "Community Day 2027",
        [
            ("Giulia Bertoldi", "Blazor from server to browser", "Room A"),
            ("Francesca Delli Santi", "My model lied in production", "Room B"),
            ("Marco Esposito", "Containers in a hurry", "Room A"),
            ("Aisha Okafor", "Accessibility that holds up", "Room B"),
            ("Luca Pellegrino", "One year later", "Room C"),
            ("Sofia Kowalczyk", "Event sourcing without drama", "Room C"),
        ],
        [("The demo fails and is rescued live", 10), ("Live coding works on the first try", 8), ("Runs over by five minutes", -3)]);

    public static TestScenario Tournament { get; } = new(
        "amateur-tournament",
        "Five-a-side Cup",
        [
            ("Red Lions", "Bologna", "Group A"),
            ("Blue Sharks", "Modena", "Group A"),
            ("Green Owls", "Parma", "Group B"),
            ("Yellow Bees", "Ferrara", "Group B"),
        ],
        [("Goal", 3), ("Comeback win", 8), ("Yellow card", -2)]);
}

public sealed record SeededGame(Guid Id, string JoinCode, IReadOnlyList<Guid> ElementIds, IReadOnlyList<Guid> RuleIds, string RefereeToken);

/// <summary>Writes test games straight into the E2E database (never a shared one).</summary>
public static class TestData
{
    private static readonly string[] Rivals = ["LaPrimaFila", "Zia Carla", "RubberDuckRita"];

    public static async Task<SeededGame> SeedGameAsync(string connectionString, TestScenario scenario, TestGameState state, bool consentPending = false)
    {
        await using var db = CreateContext(connectionString);
        var now = DateTimeOffset.UtcNow;
        var gameId = Guid.NewGuid();
        var joinCode = Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var game = new Game(gameId, $"e2e-{gameId:N}", scenario.GameName, scenario.DomainPackId, "fantaroster", "it", 3, true, 2m, joinCode, now);
        game.Open();
        db.Games.Add(game);

        var elements = scenario.Elements
            .Select(e => new Element(Guid.NewGuid(), gameId, null, null, e.Name, e.Subtitle, null, e.Group, consentPending ? ConsentStatus.Pending : ConsentStatus.NotRequired))
            .ToList();
        db.Elements.AddRange(elements);

        var rules = scenario.Rules
            .Select((r, i) => new Rule(Guid.NewGuid(), gameId, r.Label, r.Points, "General", RuleTarget.Element, i))
            .ToList();
        db.Rules.AddRange(rules);

        // Other participants with full lineups, so the leaderboard has rivals.
        for (var i = 0; i < Rivals.Length; i++)
        {
            var participant = new Participant(Guid.NewGuid(), gameId, Rivals[i], $"seed-{Guid.NewGuid():N}", now);
            db.Participants.Add(participant);
            var picks = elements.Skip(i % 2).Take(3).Select(e => e.Id).ToArray();
            db.Lineups.Add(new Lineup(participant.Id, picks, picks[0], now));
        }

        if (state is TestGameState.Live or TestGameState.Ended)
        {
            game.GoLive(now);
        }

        if (state == TestGameState.Ended)
        {
            game.End(now);
        }

        var refereeToken = $"e2e-referee-{Guid.NewGuid():N}";
        db.Referees.Add(new Referee(Guid.NewGuid(), gameId, "E2E referee", RefereeTokens.Hash(refereeToken), []));

        await db.SaveChangesAsync();
        return new SeededGame(gameId, joinCode, elements.Select(e => e.Id).ToList(), rules.Select(r => r.Id).ToList(), refereeToken);
    }

    /// <summary>Moves a game forward, as the organizer would from the console.</summary>
    public static async Task SetStateAsync(string connectionString, Guid gameId, TestGameState state)
    {
        await using var db = CreateContext(connectionString);
        var game = await db.Games.SingleAsync(g => g.Id == gameId);
        var now = DateTimeOffset.UtcNow;
        if (state is TestGameState.Live or TestGameState.Ended && game.State == GameState.Open)
        {
            game.GoLive(now);
            foreach (var lineup in await db.Lineups.Where(l => db.Participants.Any(p => p.Id == l.ParticipantId && p.GameId == gameId)).ToListAsync())
            {
                lineup.Lock();
            }
        }
        if (state == TestGameState.Ended && game.State == GameState.Live)
        {
            game.End(now);
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Assigns one point per rule to the first elements through the referee API, so the server
    /// recomputes the leaderboard and pushes the update like in a real game.
    /// </summary>
    public static async Task AddScoresAsync(Uri baseUrl, SeededGame game)
    {
        using var http = new HttpClient { BaseAddress = baseUrl };
        for (var i = 0; i < game.RuleIds.Count; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"/api/games/{game.Id}/score-entries", UriKind.Relative))
            {
                Content = JsonContent.Create(new { ElementId = game.ElementIds[i], ParticipantId = (Guid?)null, RuleId = game.RuleIds[i] }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", game.RefereeToken);
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            using var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }

    /// <summary>One more point through the referee API.</summary>
    public static async Task AddOneScoreAsync(Uri baseUrl, SeededGame game, Guid elementId, Guid ruleId)
    {
        using var http = new HttpClient { BaseAddress = baseUrl };
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"/api/games/{game.Id}/score-entries", UriKind.Relative))
        {
            Content = JsonContent.Create(new { ElementId = elementId, ParticipantId = (Guid?)null, RuleId = ruleId }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", game.RefereeToken);
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public static RosterDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<RosterDbContext>().UseSqlServer(connectionString).Options);
}
