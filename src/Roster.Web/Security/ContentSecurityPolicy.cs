namespace Roster.Web.Security;

/// <summary>
/// The Content Security Policy of the site. Blazor needs 'wasm-unsafe-eval' for WebAssembly and a WebSocket to the server;
/// everything else is the site itself: scripts, styles and fonts are served from here, and there are no inline scripts.
/// </summary>
public static class ContentSecurityPolicy
{
    /// <param name="extraConnectSources">
    /// Other places the browser must reach, such as the Azure SignalR Service when it carries the game hub.
    /// </param>
    /// <param name="nonce">A value used once per response: it allows the one inline script Blazor writes (the import map).</param>
    public static string Build(string nonce, IEnumerable<string>? extraConnectSources = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        var connect = new List<string> { "'self'" };
        connect.AddRange(extraConnectSources ?? []);

        var directives = new[]
        {
            "default-src 'self'",
            $"script-src 'self' 'nonce-{nonce}' 'wasm-unsafe-eval'",
            "style-src 'self'",
            // Element pictures come from whatever source the organizer imports from.
            "img-src 'self' data: https:",
            "font-src 'self'",
            $"connect-src {string.Join(' ', connect)}",
            "manifest-src 'self'",
            "worker-src 'self'",
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'",
        };
        return string.Join("; ", directives);
    }

    /// <summary>The origins (https and wss) of an Azure SignalR connection string, or nothing when it is not configured.</summary>
    public static IEnumerable<string> ConnectSourcesFor(string? azureSignalRConnectionString)
    {
        if (string.IsNullOrWhiteSpace(azureSignalRConnectionString))
        {
            yield break;
        }

        foreach (var part in azureSignalRConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(part["Endpoint=".Length..], UriKind.Absolute, out var endpoint)
                && endpoint.Scheme == Uri.UriSchemeHttps)
            {
                yield return $"https://{endpoint.Authority}";
                yield return $"wss://{endpoint.Authority}";
            }
        }
    }
}
