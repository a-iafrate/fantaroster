using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Roster.Web.Options;

namespace Roster.Web.Brand;

/// <summary>Chooses the brand for a request from its host. The brand is configuration, never code.</summary>
public sealed class BrandResolver
{
    private static readonly BrandDefinition[] Defaults =
    [
        new() { Id = "fantaroster", Name = "FantaRoster", DefaultCulture = "it", Hosts = ["fantaroster.*"] },
        new() { Id = "imagiroster", Name = "ImagiRoster", DefaultCulture = "en", Hosts = ["imagiroster.*"] },
    ];

    private readonly IReadOnlyList<(BrandDefinition Brand, Regex Pattern)> _patterns;

    public BrandResolver(IOptions<BrandOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var configured = options.Value;
        All = configured.Brands.Count > 0 ? configured.Brands : [.. Defaults];
        Default = All.FirstOrDefault(b => string.Equals(b.Id, configured.DefaultBrandId, StringComparison.OrdinalIgnoreCase)) ?? All[0];
        _patterns = [.. All.SelectMany(b => b.Hosts.Select(h => (b, ToRegex(h))))];
    }

    public IReadOnlyList<BrandDefinition> All { get; }

    public BrandDefinition Default { get; }

    /// <summary>The brand for a host such as "www.imagiroster.com" or "imagiroster.localhost:5000"; the default one when none matches.</summary>
    public BrandDefinition Resolve(string? host)
    {
        var name = StripPort(host);
        if (name.Length > 0)
        {
            foreach (var (brand, pattern) in _patterns)
            {
                if (pattern.IsMatch(name))
                {
                    return brand;
                }
            }
        }
        return Default;
    }

    /// <summary>The brand a game was created under; the default one when the id is unknown (a brand that was removed).</summary>
    public BrandDefinition GetById(string? id) =>
        All.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Default;

    private static string StripPort(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        var value = host.Trim();
        // IPv6 literals look like [::1]:5000; they never match a brand.
        if (value.StartsWith('['))
        {
            return string.Empty;
        }

        var colon = value.LastIndexOf(':');
        return (colon >= 0 ? value[..colon] : value).ToLowerInvariant();
    }

    private static Regex ToRegex(string hostPattern)
    {
        // Any subdomain may come first; "*" stands for the rest of the host (the domain suffix).
        var escaped = Regex.Escape(hostPattern.Trim().ToLowerInvariant()).Replace("\\*", ".+", StringComparison.Ordinal);
        return new Regex($"^(?:[a-z0-9-]+\\.)*{escaped}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
