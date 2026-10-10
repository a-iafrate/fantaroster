using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Roster.Web.Components.Pages.Organizer;

public class GameWizardState
{
    // Step 1
    [Required]
    public string DomainPackId { get; set; } = string.Empty;

    // Step 2
    [Required(ErrorMessage = "NameRequired")]
    public string Name { get; set; } = string.Empty;
    public string Culture { get; set; } = "en";
    public int LineupSize { get; set; } = 3;
    public bool CaptainEnabled { get; set; } = true;
    public decimal CaptainMultiplier { get; set; } = 2.0m;

    // Step 3
    /// <summary>A source plugin id, or "manual" to add elements by hand.</summary>
    public string SourceType { get; set; } = "manual";

    // For Sessionize (legacy binding, can keep for compatibility or remove, better replace with dictionary)
    public string? SessionizeApiId { get; set; }

    // For any plugin
    public Dictionary<string, string> PluginConfig { get; set; } = new();
}
