using System.Security.Cryptography;
using System.Text;

namespace Roster.Application.Services;

/// <summary>Referee invite tokens: random, shown once to the organizer, stored only as a hash.</summary>
public static class RefereeTokens
{
    /// <summary>A new URL-safe random token.</summary>
    public static string Create() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
