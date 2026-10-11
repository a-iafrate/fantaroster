using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Roster.Web.Security;

/// <summary>Who administers the service: the addresses listed in the Admin:Emails setting.</summary>
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string[] Emails { get; set; } = [];
}

public sealed class AdminAccess(IOptions<AdminOptions> options)
{
    public const string PolicyName = "Admin";

    public bool IsAdmin(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var email = user.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.Email) : null;
        return !string.IsNullOrWhiteSpace(email)
            && options.Value.Emails.Any(a => string.Equals(a.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class AdminRequirement : IAuthorizationRequirement;

public sealed class AdminHandler(AdminAccess access) : AuthorizationHandler<AdminRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (access.IsAdmin(context.User))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
