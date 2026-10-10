using System;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Roster.Web.Client.Pages.BigScreen;
using Roster.Web.Client.Services;
using Shouldly;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using System.Net.Http;

namespace Roster.Web.Tests.Pages.BigScreen;

public class BigScreenTests : TestContext
{
    public BigScreenTests()
    {
        var logger = Substitute.For<ILogger<GameHubClient>>();

        // Mock Http to not fail on load
        var mockHttp = new MockHttpMessageHandler();
        var client = new HttpClient(mockHttp) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(client);
        Services.AddLocalization(options => options.ResourcesPath = "Resources");
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(sp =>
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            return new GameHubClient(nav, logger);
        });
    }

    [Fact]
    public void BigScreen_renders_the_stage_with_its_controls()
    {
        var gameId = Guid.NewGuid();
        var cut = RenderComponent<Roster.Web.Client.Pages.BigScreen.BigScreen>(parameters => parameters
            .Add(p => p.GameId, gameId));

        cut.Find(".stage").ShouldNotBeNull();
        cut.Find("[role=toolbar]").ShouldNotBeNull();
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{}")
            });
        }
    }
}
