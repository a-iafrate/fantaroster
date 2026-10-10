using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Roster.Web.Client.Components.Participant;

public static class ParticipantFormat
{
    /// <summary>Signed points with a real minus sign: "+10", "−3", "0".</summary>
    public static string Points(int points) => points.ToString("+#;−#;0", CultureInfo.InvariantCulture);

    /// <summary>Captain multiplier, e.g. "×2" or "×1.5" (culture-aware decimal separator).</summary>
    public static string Multiplier(decimal multiplier) => "×" + multiplier.ToString("0.##", CultureInfo.CurrentCulture);

    /// <summary>Short relative time: "now", "4 min", "2 h", then a short date.</summary>
    public static string Ago(DateTimeOffset value, DateTimeOffset now, IStringLocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        var elapsed = now - value;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return localizer["TimeNow"];
        }
        if (elapsed < TimeSpan.FromHours(1))
        {
            return string.Format(CultureInfo.CurrentCulture, localizer["TimeMinutes"], (int)elapsed.TotalMinutes);
        }
        if (elapsed < TimeSpan.FromDays(1))
        {
            return string.Format(CultureInfo.CurrentCulture, localizer["TimeHours"], (int)elapsed.TotalHours);
        }
        return value.ToLocalTime().ToString("d MMM", CultureInfo.CurrentCulture);
    }
}
