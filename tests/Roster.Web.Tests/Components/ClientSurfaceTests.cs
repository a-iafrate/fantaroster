using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Roster.Web.Client;
using Roster.Web.Client.Layout;
using Shouldly;
using Xunit;

namespace Roster.Web.Tests.Components;

public sealed class ClientSurfaceTests : TestContext
{
    public ClientSurfaceTests()
    {
        Services.AddLocalization(options => options.ResourcesPath = "Resources");
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public static TheoryData<Type> RoutablePages()
    {
        var data = new TheoryData<Type>();
        foreach (var type in typeof(ClientRenderMode).Assembly.GetTypes()
                     .Where(t => t.GetCustomAttributes<RouteAttribute>().Any())
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            data.Add(type);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(RoutablePages))]
    public void Every_client_page_is_interactive_webassembly(Type page)
    {
        // Without a render mode a page in Roster.Web.Client is rendered as static HTML:
        // no clicks and no real-time updates.
        var renderMode = page.GetCustomAttribute<RenderModeAttribute>(inherit: true);

        renderMode.ShouldNotBeNull($"{page.Name} has no @rendermode");
        renderMode.Mode.ShouldBeOfType<InteractiveWebAssemblyRenderMode>();
    }

    [Fact]
    public void StageLayout_pins_the_stage_theme()
    {
        var cut = RenderComponent<StageLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "body"))));

        var surface = cut.Find("[data-theme-surface]");
        surface.GetAttribute("data-theme-surface").ShouldBe("stage");
        surface.GetAttribute("data-theme-lock").ShouldBe("stage");
        surface.TextContent.ShouldContain("body");
    }

    [Fact]
    public void RefereeLayout_defaults_to_the_dark_theme()
    {
        var cut = RenderComponent<RefereeLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "body"))));

        var surface = cut.Find("[data-theme-surface]");
        surface.GetAttribute("data-theme-surface").ShouldBe("referee");
        surface.GetAttribute("data-theme-default").ShouldBe("dark");
        surface.HasAttribute("data-theme-lock").ShouldBeFalse();
    }

    [Fact]
    public void ParticipantLayout_follows_the_device_theme()
    {
        var cut = RenderComponent<ParticipantLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "body"))));

        var surface = cut.Find("[data-theme-surface]");
        surface.GetAttribute("data-theme-surface").ShouldBe("participant");
        surface.HasAttribute("data-theme-default").ShouldBeFalse();
        surface.HasAttribute("data-theme-lock").ShouldBeFalse();
        cut.Find("#blazor-error-ui .dismiss").GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
    }
}
