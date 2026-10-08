using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Roster.Plugins.Abstractions;
using Roster.Plugins.Sessionize;
using Shouldly;
using Xunit;

namespace Roster.Plugins.Tests;

public class SessionizeElementSourcePluginTests
{
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task ValidateConfigAsync_WithMissingEndpointId_ShouldFail()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler(""));
        var plugin = new SessionizeElementSourcePlugin(httpClient);

        var config = new PluginConfig(new Dictionary<string, string>());
        var result = await plugin.ValidateConfigAsync(config, CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("Endpoint ID is required"));
    }

    [Fact]
    public async Task ImportAsync_WithValidEndpoint_ShouldParseSpeakers()
    {
        var jsonContent = await File.ReadAllTextAsync("Fixtures/sessionize-all.json");
        var httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        var plugin = new SessionizeElementSourcePlugin(httpClient);

        var config = new PluginConfig(new Dictionary<string, string>
        {
            { "EndpointId", "mock-endpoint" }
        });

        var result = await plugin.ImportAsync(config, CancellationToken.None);

        result.Warnings.ShouldBeEmpty();
        result.Elements.Count.ShouldBe(2);

        var first = result.Elements[0];
        first.ExternalId.ShouldBe("spk-abc-123");
        first.Name.ShouldBe("John Doe");
        first.Subtitle.ShouldBe("Senior Engineer");
        first.ImageUrl.ShouldBe("https://example.com/johndoe.jpg");
        first.Group.ShouldBe("Speaker");
    }

    [Fact]
    public async Task ImportAsync_WithImportSessionsChecked_ShouldParseSessionsToo()
    {
        var jsonContent = await File.ReadAllTextAsync("Fixtures/sessionize-all.json");
        var httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        var plugin = new SessionizeElementSourcePlugin(httpClient);

        var config = new PluginConfig(new Dictionary<string, string>
        {
            { "EndpointId", "mock-endpoint" },
            { "ImportSessions", "true" }
        });

        var result = await plugin.ImportAsync(config, CancellationToken.None);

        result.Warnings.ShouldBeEmpty();
        result.Elements.Count.ShouldBe(3); // 2 speakers + 1 session

        var session = result.Elements[2];
        session.ExternalId.ShouldBe("sess-sess-999");
        session.Name.ShouldBe("Building Awesome APIs");
        session.Group.ShouldBe("Session");
    }

    [Fact]
    public async Task ImportAsync_WhenEndpointReturns404_ShouldReturnError()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler("", HttpStatusCode.NotFound));
        var plugin = new SessionizeElementSourcePlugin(httpClient);

        var config = new PluginConfig(new Dictionary<string, string>
        {
            { "EndpointId", "mock-endpoint" }
        });

        var result = await plugin.ImportAsync(config, CancellationToken.None);

        result.Elements.ShouldBeEmpty();
        result.Warnings.ShouldContain(w => w.Contains("not found or is not enabled"));
    }
}
