namespace Roster.Web.Options;

/// <summary>One brand: identity, default language, the hosts it answers on and the colors of its PWA.</summary>
public sealed class BrandDefinition
{
    /// <summary>Stable identifier stored on every game (never shown to people).</summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Language used until the person chooses one.</summary>
    public string DefaultCulture { get; set; } = "en";

    /// <summary>Host patterns, case-insensitive. "name.*" matches name.com, www.name.it and name.localhost.</summary>
    public List<string> Hosts { get; set; } = [];

    public string ThemeColor { get; set; } = "#5000A5";

    public string BackgroundColor { get; set; } = "#F8F9FA";

    public string LogoPath { get; set; } = "assets/logo-r.svg";
}
