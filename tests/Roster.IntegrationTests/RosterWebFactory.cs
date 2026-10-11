using Microsoft.AspNetCore.Mvc.Testing;

namespace Roster.IntegrationTests;

/// <summary>
/// The web app against the test database, with every remote service switched off: no Azure SQL, no Azure SignalR,
/// no email. Settings go through environment variables because the app reads its configuration while it builds.
/// </summary>
public sealed class RosterWebFactory : WebApplicationFactory<Program>
{
    static RosterWebFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__sqldb", IntegrationDatabase.ConnectionString);
        Environment.SetEnvironmentVariable("Azure__SignalR__ConnectionString", string.Empty);
        Environment.SetEnvironmentVariable("AzureCommunicationServices__ConnectionString", string.Empty);
        // A small limit so the rate limit tests need a handful of requests.
        Environment.SetEnvironmentVariable("Security__RateLimits__JoinCodeLookupPerMinute", "3");
    }
}
