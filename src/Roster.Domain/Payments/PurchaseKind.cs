namespace Roster.Domain.Payments;

/// <summary>What a one-time payment buys for a single game.</summary>
public enum PurchaseKind
{
    /// <summary>The per-game plan for parties, tournaments and groups of friends.</summary>
    Premium,

    /// <summary>The per-event plan for commercial and corporate events.</summary>
    Pro,
}
