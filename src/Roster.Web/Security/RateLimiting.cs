using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

namespace Roster.Web.Security;

/// <summary>
/// Limits per kind of request. Participants at an event often share one public address (the venue Wi-Fi), so the
/// limits that protect many-people endpoints are generous; the ones that guard secrets (sign in, codes, tokens) are tight.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "Security:RateLimits";

    /// <summary>Sign-in form posts per address in ten minutes (each one sends an email).</summary>
    public int LoginPer10Minutes { get; set; } = 5;

    /// <summary>Magic-link verifications per address in five minutes.</summary>
    public int MagicLinkVerifyPer5Minutes { get; set; } = 10;

    /// <summary>Game code lookups per address per minute: stops anyone from walking through all the codes.</summary>
    public int JoinCodeLookupPerMinute { get; set; } = 120;

    /// <summary>Joins per address per minute: a whole room joining behind one venue address must still fit.</summary>
    public int JoinPerMinute { get; set; } = 300;

    /// <summary>Points assigned or voided per referee token per minute.</summary>
    public int ScoreWritesPerMinute { get; set; } = 90;

    /// <summary>Referee token checks per address per minute.</summary>
    public int RefereeCheckPerMinute { get; set; } = 30;

    /// <summary>Consent page requests per address per minute.</summary>
    public int ConsentPerMinute { get; set; } = 30;
}

public static class RateLimiting
{
    /// <summary>What limits a request: the policy name, how many it allows and per how long. Null means not limited.</summary>
    public readonly record struct Rule(string Policy, int Permits, TimeSpan Window);

    public static PartitionedRateLimiter<HttpContext> Create(RateLimitOptions options) =>
        PartitionedRateLimiter.Create<HttpContext, string>(context => Partition(context, options));

    public static RateLimitPartition<string> Partition(HttpContext context, RateLimitOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);

        var rule = Classify(context.Request.Method, context.Request.Path.Value ?? string.Empty, options);
        if (rule is null)
        {
            return RateLimitPartition.GetNoLimiter("unlimited");
        }

        var key = $"{rule.Value.Policy}:{Identify(context, rule.Value.Policy)}";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rule.Value.Permits,
            Window = rule.Value.Window,
            QueueLimit = 0,
        });
    }

    /// <summary>Pure: picks the rule from the method and the path.</summary>
    public static Rule? Classify(string method, string path, RateLimitOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var isPost = HttpMethods.IsPost(method);
        var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (path.Equals("/login", StringComparison.OrdinalIgnoreCase) && isPost)
        {
            return new Rule("login", options.LoginPer10Minutes, TimeSpan.FromMinutes(10));
        }

        if (path.Equals("/auth/verify", StringComparison.OrdinalIgnoreCase))
        {
            return new Rule("magic-link-verify", options.MagicLinkVerifyPer5Minutes, TimeSpan.FromMinutes(5));
        }

        if (segments is ["api", "join-codes", _])
        {
            return new Rule("join-code", options.JoinCodeLookupPerMinute, TimeSpan.FromMinutes(1));
        }

        if (isPost && segments is ["api", "games", _, "participant", "join"])
        {
            return new Rule("join", options.JoinPerMinute, TimeSpan.FromMinutes(1));
        }

        if (isPost && segments is ["api", "games", _, "score-entries", ..])
        {
            return new Rule("score-write", options.ScoreWritesPerMinute, TimeSpan.FromMinutes(1));
        }

        if (segments is ["api", "games", _, "referee", "me"])
        {
            return new Rule("referee-check", options.RefereeCheckPerMinute, TimeSpan.FromMinutes(1));
        }

        if (segments is ["consent", _])
        {
            return new Rule("consent", options.ConsentPerMinute, TimeSpan.FromMinutes(1));
        }

        return null;
    }

    /// <summary>Referee writes are limited per token (a referee behind a shared address is still one referee); everything else per address.</summary>
    private static string Identify(HttpContext context, string policy)
    {
        if (policy == "score-write"
            && context.Request.Headers.Authorization.ToString() is { Length: > 7 } header
            && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return "token-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(header["Bearer ".Length..].Trim())))[..16];
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
