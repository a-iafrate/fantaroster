namespace Roster.Web.Client.Components.Participant;

public static class ParticipantRoutes
{
    public static string Home(Guid gameId) => $"/games/{gameId}";

    public static string Join(Guid gameId) => $"/games/{gameId}/join";

    public static string For(Guid gameId, ParticipantTab tab) => tab switch
    {
        ParticipantTab.Leaderboard => $"/games/{gameId}/leaderboard",
        ParticipantTab.Lineup => $"/games/{gameId}/lineup",
        ParticipantTab.Feed => $"/games/{gameId}/activity",
        ParticipantTab.Rules => $"/games/{gameId}/rules",
        _ => Home(gameId),
    };
}
