using System;
using System.Collections.Generic;
using System.Linq;
using Roster.Domain.Games;
using Roster.Domain.Participants;
using Roster.Domain.Scores;

namespace Roster.Domain.Scoring;

public sealed record ElementScore(Guid ElementId, int Score);

public sealed record ParticipantScore(Guid ParticipantId, int Score, int Rank);

public sealed record Leaderboard(
    IReadOnlyList<ParticipantScore> Participants,
    IReadOnlyList<ElementScore> Elements
);

public static class ScoringEngine
{
    public static Leaderboard CalculateLeaderboard(
        Game game,
        IEnumerable<ScoreEntry> validEntries,
        IEnumerable<Lineup> lineups)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(validEntries);
        ArgumentNullException.ThrowIfNull(lineups);

        // 1. Calculate Element scores
        var elementScoresDict = new Dictionary<Guid, int>();

        var elementEntries = validEntries.Where(e => e.ElementId.HasValue && e.Status == ScoreStatus.Valid);
        foreach (var entry in elementEntries)
        {
            var elId = entry.ElementId!.Value;
            if (!elementScoresDict.ContainsKey(elId))
                elementScoresDict[elId] = 0;

            elementScoresDict[elId] += entry.PointsSnapshot;
        }

        var elementScores = elementScoresDict
            .Select(kv => new ElementScore(kv.Key, kv.Value))
            .OrderByDescending(x => x.Score)
            .ToList();

        // 2. Calculate Participant scores
        var participantScoresDict = new Dictionary<Guid, int>();

        // Ensure all participants with a lineup exist in the dictionary (even with 0 points)
        foreach (var lineup in lineups)
        {
            if (!participantScoresDict.ContainsKey(lineup.ParticipantId))
            {
                participantScoresDict[lineup.ParticipantId] = 0;
            }

            int score = 0;
            foreach (var elId in lineup.PickedElementIds)
            {
                if (elementScoresDict.TryGetValue(elId, out int points))
                {
                    if (game.CaptainEnabled && elId == lineup.CaptainElementId)
                    {
                        score += (int)Math.Round(points * game.CaptainMultiplier, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        score += points;
                    }
                }
            }
            participantScoresDict[lineup.ParticipantId] += score;
        }

        // Add personal entries (e.g. SponsorBonus) targeting the participant directly
        var participantEntries = validEntries.Where(e => e.ParticipantId.HasValue && e.Status == ScoreStatus.Valid);
        foreach (var entry in participantEntries)
        {
            var pId = entry.ParticipantId!.Value;
            if (!participantScoresDict.ContainsKey(pId))
                participantScoresDict[pId] = 0;

            participantScoresDict[pId] += entry.PointsSnapshot;
        }

        // 3. Rank participants (Standard Competition Ranking: 1, 2, 2, 4)
        var orderedParticipants = participantScoresDict
            .OrderByDescending(kv => kv.Value)
            .ToList();

        var rankedParticipants = new List<ParticipantScore>();
        int currentRank = 1;

        for (int i = 0; i < orderedParticipants.Count; i++)
        {
            if (i > 0 && orderedParticipants[i].Value < orderedParticipants[i - 1].Value)
            {
                currentRank = i + 1;
            }

            rankedParticipants.Add(new ParticipantScore(orderedParticipants[i].Key, orderedParticipants[i].Value, currentRank));
        }

        return new Leaderboard(rankedParticipants, elementScores);
    }
}
