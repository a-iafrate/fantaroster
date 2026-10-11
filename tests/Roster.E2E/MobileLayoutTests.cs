using Microsoft.Playwright;
using Shouldly;

namespace Roster.E2E;

[Collection(E2EGroup.Name)]
public sealed class MobileLayoutTests(RosterAppFixture app)
{
    [Theory]
    [InlineData("/", 360)]
    [InlineData("/", 390)]
    [InlineData("/j", 360)]
    [InlineData("/j", 390)]
    [InlineData("/login", 360)]
    [InlineData("/login", 390)]
    public async Task Page_has_no_horizontal_overflow_on_phones(string path, int width)
    {
        var page = await app.NewPageAsync(width: width);

        await page.GotoAsync(path);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var overflow = await page.EvaluateAsync<int>(
            "() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        var culprits = overflow == 0 ? string.Empty : await page.EvaluateAsync<string>(
            """
            () => [...document.querySelectorAll('body *')]
                .filter(e => e.getBoundingClientRect().right > document.documentElement.clientWidth + 1)
                .slice(0, 5)
                .map(e => `${e.tagName.toLowerCase()}.${e.className} (${Math.round(e.getBoundingClientRect().width)}px)`)
                .join(', ')
            """);
        overflow.ShouldBe(0, $"{path} overflows horizontally at {width}px: {culprits}");
    }
}
