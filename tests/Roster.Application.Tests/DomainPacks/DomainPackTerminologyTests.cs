using System.Globalization;
using Roster.DomainPacks;
using Shouldly;
using Xunit;

namespace Roster.Application.Tests.DomainPacks;

public sealed class DomainPackTerminologyTests
{
    private readonly DomainPackLoader _loader = new();

    [Theory]
    [InlineData("generic")]
    [InlineData("tech-conference")]
    [InlineData("amateur-tournament")]
    public void Every_pack_defines_terminology_in_english_and_italian(string packId)
    {
        var pack = _loader.GetPack(packId);
        pack.ShouldNotBeNull();

        foreach (var culture in new[] { "en", "it" })
        {
            var terms = pack.GetTerminology(new CultureInfo(culture));
            terms.ElementSingular.ShouldNotBeNullOrWhiteSpace();
            terms.ElementPlural.ShouldNotBeNullOrWhiteSpace();
            terms.GroupLabel.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void GetTerminology_uses_the_parent_language_of_a_regional_culture()
    {
        var pack = _loader.GetPack("generic")!;

        pack.GetTerminology(new CultureInfo("it-IT")).ShouldBe(pack.Terminology["it"]);
    }

    [Fact]
    public void GetTerminology_falls_back_to_english_for_an_unknown_language()
    {
        var pack = _loader.GetPack("generic")!;

        pack.GetTerminology(new CultureInfo("de-DE")).ShouldBe(pack.Terminology["en"]);
    }

    [Fact]
    public void GetTerminology_falls_back_to_the_first_terminology_without_english()
    {
        var italianOnly = new PackTerminology("Elemento", "Elementi", "Gruppo");
        var pack = new DomainPack(
            "test", new Dictionary<string, string>(), new Dictionary<string, string>(), false, [],
            new Dictionary<string, PackTerminology> { ["it"] = italianOnly }, []);

        pack.GetTerminology(new CultureInfo("fr")).ShouldBe(italianOnly);
    }
}
