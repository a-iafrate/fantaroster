using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Roster.Infrastructure.Data;

namespace Roster.Infrastructure.Security;

/// <summary>Adds the organizer display name to the sign-in cookie so the UI can show it without a database read.</summary>
public sealed class OrganizerClaimsPrincipalFactory(
    UserManager<OrganizerUser> userManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<OrganizerUser>(userManager, options)
{
    public const string DisplayNameClaimType = "display_name";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(OrganizerUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            identity.AddClaim(new Claim(DisplayNameClaimType, user.DisplayName));

        return identity;
    }
}
