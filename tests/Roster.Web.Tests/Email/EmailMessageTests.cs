using Roster.Web.Email;
using Shouldly;
using Xunit;

namespace Roster.Web.Tests.Email;

public class EmailMessageTests
{
    private static EmailContentModel Model(string brandColor = "#5000A5", string heading = "Sign in to Brand") => new(
        "Brand",
        brandColor,
        "en",
        heading,
        ["Press the button to sign in.", "If it was not you,\nignore this."],
        "Sign in",
        "https://example.test/auth/verify?email=a%40b.test&token=x&y=1",
        "If the button does not work, copy this address:",
        "Brand · Anything");

    [Fact]
    public void ToHtml_HasTheHeadingTheButtonAndTheFallbackLink()
    {
        var html = EmailMessage.ToHtml(Model());

        html.ShouldContain("<title>Sign in to Brand</title>");
        html.ShouldContain("Sign in</a>");
        html.ShouldContain("href=\"https://example.test/auth/verify?email=a%40b.test&amp;token=x&amp;y=1\"");
        html.ShouldContain("If the button does not work, copy this address:");
        html.ShouldContain("#5000A5");
        html.ShouldContain("ignore this.");
        html.ShouldContain("<br>");
    }

    [Fact]
    public void ToHtml_EncodesTextThatComesFromPeople()
    {
        var html = EmailMessage.ToHtml(Model(heading: "<script>alert(1)</script>"));

        html.ShouldNotContain("<script>");
        html.ShouldContain("&lt;script&gt;");
    }

    [Theory]
    [InlineData("red; background:url(x)")]
    [InlineData("#12")]
    [InlineData("")]
    public void ToHtml_AnUnsafeBrandColorFallsBackToTheDefault(string color)
    {
        var html = EmailMessage.ToHtml(Model(brandColor: color));

        html.ShouldContain("#5000A5");
        html.ShouldNotContain("url(x)");
    }

    [Fact]
    public void ToPlainText_CarriesTheSameContentWithTheLink()
    {
        var text = EmailMessage.ToPlainText(Model());

        text.ShouldContain("Sign in to Brand");
        text.ShouldContain("Press the button to sign in.");
        text.ShouldContain("Sign in: https://example.test/auth/verify?email=a%40b.test&token=x&y=1");
        text.ShouldContain("Brand · Anything");
    }
}
