using Microsoft.Extensions.Options;
using Roster.Web.Brand;
using Roster.Web.Options;
using Shouldly;
using Xunit;

namespace Roster.Web.Tests.Components;

public sealed class BrandResolverTests
{
    private static BrandResolver Create(BrandOptions? options = null) =>
        new(Microsoft.Extensions.Options.Options.Create(options ?? new BrandOptions()));

    [Theory]
    [InlineData("fantaroster.com", "fantaroster")]
    [InlineData("www.fantaroster.com", "fantaroster")]
    [InlineData("FANTAROSTER.IT", "fantaroster")]
    [InlineData("staging.fantaroster.com:8443", "fantaroster")]
    [InlineData("fantaroster.localhost:5038", "fantaroster")]
    [InlineData("imagiroster.com", "imagiroster")]
    [InlineData("app.imagiroster.net", "imagiroster")]
    [InlineData("imagiroster.localhost:5099", "imagiroster")]
    public void Resolve_picks_the_brand_from_the_host(string host, string expected) =>
        Create().Resolve(host).Id.ShouldBe(expected);

    [Theory]
    [InlineData("localhost:5038")]
    [InlineData("127.0.0.1:5000")]
    [InlineData("example.org")]
    [InlineData("notimagiroster.com")]
    [InlineData("imagiroster")]
    [InlineData("[::1]:5000")]
    [InlineData("")]
    [InlineData(null)]
    public void Resolve_falls_back_to_the_default_brand(string? host) =>
        Create().Resolve(host).Id.ShouldBe("fantaroster");

    [Fact]
    public void Default_brand_is_configurable()
    {
        var resolver = Create(new BrandOptions { DefaultBrandId = "imagiroster" });

        resolver.Resolve("localhost").Id.ShouldBe("imagiroster");
        resolver.Default.DefaultCulture.ShouldBe("en");
    }

    [Fact]
    public void Configured_brands_replace_the_built_in_ones()
    {
        var resolver = Create(new BrandOptions
        {
            DefaultBrandId = "acme",
            Brands = [new BrandDefinition { Id = "acme", Name = "Acme", DefaultCulture = "it", Hosts = ["acme.*"] }],
        });

        resolver.All.Count.ShouldBe(1);
        resolver.Resolve("www.acme.io").Name.ShouldBe("Acme");
        resolver.Resolve("fantaroster.com").Id.ShouldBe("acme");
    }

    [Fact]
    public void An_unknown_default_falls_back_to_the_first_brand()
    {
        Create(new BrandOptions { DefaultBrandId = "gone" }).Default.Id.ShouldBe("fantaroster");
    }

    [Theory]
    [InlineData("imagiroster", "imagiroster")]
    [InlineData("IMAGIROSTER", "imagiroster")]
    [InlineData("removed-brand", "fantaroster")]
    [InlineData(null, "fantaroster")]
    public void GetById_returns_the_brand_a_game_was_created_under(string? id, string expected) =>
        Create().GetById(id).Id.ShouldBe(expected);

    [Theory]
    [InlineData("/organizer", true)]
    [InlineData("/", true)]
    [InlineData("/games/1/join?x=1", true)]
    [InlineData("//other.example", false)]
    [InlineData(@"/\other.example", false)]
    [InlineData("https://other.example", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Language_switch_only_returns_to_this_site(string? url, bool expected) =>
        CultureEndpoints.IsLocal(url).ShouldBe(expected);
}
