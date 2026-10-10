using System.Globalization;

namespace Roster.DomainPacks;

public static class DomainPackExtensions
{
    /// <summary>
    /// Returns the terminology for the given culture, falling back to its parent language,
    /// then English, then the first terminology defined by the pack.
    /// </summary>
    public static PackTerminology GetTerminology(this DomainPack pack, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(culture);

        if (pack.Terminology.TryGetValue(culture.Name, out var exact))
        {
            return exact;
        }

        if (pack.Terminology.TryGetValue(culture.TwoLetterISOLanguageName, out var language))
        {
            return language;
        }

        if (pack.Terminology.TryGetValue("en", out var english))
        {
            return english;
        }

        return pack.Terminology.Values.FirstOrDefault()
            ?? throw new InvalidOperationException($"Domain pack '{pack.Id}' defines no terminology.");
    }
}
