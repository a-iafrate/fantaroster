namespace Roster.Ui.Components;

/// <summary>Initials and avatar color for anything in play: a person, a group, a song, a dish.</summary>
public static class AvatarText
{
    private const int ColorCount = 5;

    /// <summary>First letters of the first two words, or the first two letters of a single word.</summary>
    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var words = name.Split([' ', '-', '_', '.', '&'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
        {
            return string.Concat(FirstLetter(words[0]), FirstLetter(words[1])).ToUpperInvariant();
        }

        var letters = words[0].Where(char.IsLetterOrDigit).Take(2).ToArray();
        return letters.Length == 0 ? "?" : new string(letters).ToUpperInvariant();
    }

    /// <summary>A stable color index between 1 and 5 for the given seed.</summary>
    public static int ColorIndex(string? seed)
    {
        if (string.IsNullOrEmpty(seed))
        {
            return 1;
        }

        // Deterministic across runs and platforms (string.GetHashCode is randomized per process).
        var hash = 0;
        foreach (var c in seed)
        {
            hash = unchecked((hash * 31) + c);
        }
        return (int)((uint)hash % ColorCount) + 1;
    }

    private static string FirstLetter(string word)
    {
        var letter = word.FirstOrDefault(char.IsLetterOrDigit);
        return letter == default ? string.Empty : letter.ToString();
    }
}
