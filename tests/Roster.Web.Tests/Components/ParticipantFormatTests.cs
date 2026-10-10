using System.Globalization;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Roster.Web.Client.Components.Participant;
using Shouldly;
using Xunit;

namespace Roster.Web.Tests.Components;

public sealed class ParticipantFormatTests
{
    private static readonly DateTimeOffset Now = new(2027, 5, 20, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(10, "+10")]
    [InlineData(-3, "−3")]
    [InlineData(0, "0")]
    public void Points_are_signed_with_a_real_minus(int points, string expected) =>
        ParticipantFormat.Points(points).ShouldBe(expected);

    [Fact]
    public void Multiplier_uses_the_culture_decimal_separator()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("it-IT");
            ParticipantFormat.Multiplier(1.5m).ShouldBe("×1,5");
            ParticipantFormat.Multiplier(2m).ShouldBe("×2");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(20, "now")]
    [InlineData(4 * 60, "4 min")]
    [InlineData(3 * 3600, "3 h")]
    public void Ago_is_relative_for_the_last_day(int secondsAgo, string expected)
    {
        var localizer = Substitute.For<IStringLocalizer>();
        localizer["TimeNow"].Returns(new LocalizedString("TimeNow", "now"));
        localizer["TimeMinutes"].Returns(new LocalizedString("TimeMinutes", "{0} min"));
        localizer["TimeHours"].Returns(new LocalizedString("TimeHours", "{0} h"));

        ParticipantFormat.Ago(Now.AddSeconds(-secondsAgo), Now, localizer).ShouldBe(expected);
    }
}
