using Microsoft.AspNetCore.Identity;

namespace Roster.Infrastructure.Data;

public class OrganizerUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string PreferredCulture { get; set; } = "en";
    public string Plan { get; set; } = "Free";
}
