using Roster.Web.Options;

namespace Roster.Web.Brand;

/// <summary>The brand of the current request or circuit.</summary>
public interface IBrandAccessor
{
    BrandDefinition Current { get; }

    /// <summary>A brand by id, for things that belong to a brand other than the one of the host (an existing game).</summary>
    BrandDefinition ForGame(string? brandId);
}
