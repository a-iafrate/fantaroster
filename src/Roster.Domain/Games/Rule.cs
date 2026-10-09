using System;

namespace Roster.Domain.Games;

public sealed class Rule
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string Label { get; private set; }
    public int Points { get; private set; }
    public string Category { get; private set; }
    public RuleTarget Target { get; private set; }
    public int Order { get; private set; }

    private Rule()
    {
        Label = string.Empty;
        Category = string.Empty;
    }

    public Rule(Guid id, Guid gameId, string label, int points, string category, RuleTarget target, int order = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (points == 0) throw new ArgumentException("Points must be a non-zero integer.", nameof(points));

        Id = id;
        GameId = gameId;
        Label = label;
        Points = points;
        Category = category;
        Target = target;
        Order = order;
    }

    public void Update(string label, int points, string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (points == 0) throw new ArgumentException("Points must be a non-zero integer.", nameof(points));

        Label = label;
        Points = points;
        Category = category;
    }

    public void SetOrder(int order)
    {
        Order = order;
    }
}
