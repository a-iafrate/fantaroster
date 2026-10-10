using System.Text.Json.Serialization;

namespace Roster.DomainPacks;

public sealed record DomainPack(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] IReadOnlyDictionary<string, string> Name,
    [property: JsonPropertyName("description")] IReadOnlyDictionary<string, string> Description,
    [property: JsonPropertyName("requiresConsent")] bool RequiresConsent,
    [property: JsonPropertyName("recommendedPlugins")] IReadOnlyList<string> RecommendedPlugins,
    [property: JsonPropertyName("terminology")] IReadOnlyDictionary<string, PackTerminology> Terminology,
    [property: JsonPropertyName("defaultRules")] IReadOnlyList<PackRule> DefaultRules,
    [property: JsonPropertyName("icon")] string? Icon = null
);

public sealed record PackTerminology(
    [property: JsonPropertyName("elementSingular")] string ElementSingular,
    [property: JsonPropertyName("elementPlural")] string ElementPlural,
    [property: JsonPropertyName("groupLabel")] string GroupLabel
);

public sealed record PackRule(
    [property: JsonPropertyName("label")] IReadOnlyDictionary<string, string> Label,
    [property: JsonPropertyName("points")] int Points
);
