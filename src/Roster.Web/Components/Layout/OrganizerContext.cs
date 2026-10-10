using Roster.Domain.Games;

namespace Roster.Web.Components.Layout;

/// <summary>The game the organizer is looking at, as the shell needs it.</summary>
public sealed record OrganizerGameInfo(Guid Id, string Name, GameState State);

/// <summary>
/// State shared between the organizer pages and the shell (sidebar, top bar) within one circuit.
/// Pages load the data and publish it here: the shell never queries the database itself, because the
/// shell and the page render at the same time and would otherwise share one DbContext concurrently.
/// </summary>
public sealed class OrganizerContext
{
    public OrganizerGameInfo? Game { get; private set; }

    /// <summary>Elements that have not answered the consent request yet (a to-do badge, never an info count).</summary>
    public int PendingConsents { get; private set; }

    public event Action? Changed;

    public void SetGame(OrganizerGameInfo? game)
    {
        Game = game;
        if (game is null)
        {
            PendingConsents = 0;
        }
        Changed?.Invoke();
    }

    public void SetPendingConsents(int count)
    {
        if (PendingConsents != count)
        {
            PendingConsents = count;
            Changed?.Invoke();
        }
    }
}
