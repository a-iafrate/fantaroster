using Roster.Application.Services;
using Shouldly;
using Xunit;

namespace Roster.Application.Tests.Referees;

public sealed class RefereeTokensTests
{
    [Fact]
    public void Create_returns_unique_url_safe_tokens()
    {
        var tokens = Enumerable.Range(0, 50).Select(_ => RefereeTokens.Create()).ToList();

        tokens.Distinct().Count().ShouldBe(50);
        foreach (var token in tokens)
        {
            token.Length.ShouldBeGreaterThanOrEqualTo(40);
            token.All(IsUrlSafe).ShouldBeTrue(token);
        }
    }

    [Fact]
    public void Hash_is_stable_and_never_contains_the_token()
    {
        var token = RefereeTokens.Create();

        RefereeTokens.Hash(token).ShouldBe(RefereeTokens.Hash(token));
        RefereeTokens.Hash(token).ShouldNotContain(token);
        RefereeTokens.Hash(token).ShouldNotBe(RefereeTokens.Hash(RefereeTokens.Create()));
    }

    private static bool IsUrlSafe(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '_';
}
