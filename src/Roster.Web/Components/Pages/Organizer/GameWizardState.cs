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
    public string SourceType { get; set; } = "sessionize"; // "sessionize", "csv", "manual"

    // For Sessionize
    public string? SessionizeApiId { get; set; }
}
