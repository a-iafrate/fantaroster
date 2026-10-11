using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Roster.Web.Security;

/// <summary>Who the signed-in organizer is, for the pages that must only show their own games.</summary>
public static class OrganizerIdentity
{
    public static async Task<string?> GetIdAsync(this AuthenticationStateProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var user = (await provider.GetAuthenticationStateAsync()).User;
        return GetId(user);
    }

    public static string? GetId(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.NameIdentifier) : null;
    }
}

