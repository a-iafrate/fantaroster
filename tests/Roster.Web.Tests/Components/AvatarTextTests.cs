using Roster.Ui.Components;
using Shouldly;
using Xunit;

namespace Roster.Web.Tests.Components;

public sealed class AvatarTextTests
{
    [Theory]
    [InlineData("Giulia Bertoldi-Mancuso", "GB")]
    [InlineData("Red Lions", "RL")]
    [InlineData("Nonna", "NO")]
    [InlineData("  marco   esposito ", "ME")]
    [InlineData("A", "A")]
    [InlineData("", "?")]
    [InlineData(null, "?")]
    [InlineData("***", "?")]
    public void Initials_uses_the_first_letters_of_the_name(string? name, string expected) =>
        AvatarText.Initials(name).ShouldBe(expected);

    [Fact]
    public void ColorIndex_is_stable_and_within_the_palette()
    {
        var seen = new HashSet<int>();
        for (var i = 0; i < 200; i++)
        {
            var seed = Guid.NewGuid().ToString();
            var index = AvatarText.ColorIndex(seed);
            index.ShouldBeInRange(1, 5);
            AvatarText.ColorIndex(seed).ShouldBe(index);
            seen.Add(index);
        }
        seen.Count.ShouldBe(5);
    }

    [Fact]
    public void ColorIndex_does_not_change_between_runs() =>
        AvatarText.ColorIndex("element-1").ShouldBe(AvatarText.ColorIndex("element-1"));

    [Fact]
    public void ColorIndex_defaults_to_the_first_color_without_a_seed() =>
        AvatarText.ColorIndex(null).ShouldBe(1);
}
