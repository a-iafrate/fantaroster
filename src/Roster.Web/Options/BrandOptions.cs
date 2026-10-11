namespace Roster.Web.Options;

/// <summary>
/// The brands one deployment serves. When <see cref="Brands"/> is empty the built-in definitions apply,
/// so a missing configuration section never leaves the site without a brand.
/// </summary>
public sealed class BrandOptions
{
    public const string SectionName = "Brand";

    /// <summary>The brand used when the host matches none (local development, an unknown domain).</summary>
    public string DefaultBrandId { get; set; } = "fantaroster";

    public List<BrandDefinition> Brands { get; set; } = [];
}
