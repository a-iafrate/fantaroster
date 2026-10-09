using System;
using System.Linq;
using Roster.Application.Ports.Security;

namespace Roster.Infrastructure.Security;

public sealed class BasicProfanityFilter : IProfanityFilter
{
    private readonly string[] _blocklist = new[] { "admin", "moderator", "root", "system", "cazzo", "merda", "fuck", "shit" };

    public bool ContainsProfanity(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var normalized = text.ToLowerInvariant();
        return _blocklist.Any(badWord => normalized.Contains(badWord));
    }
}
