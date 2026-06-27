using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class GrokServiceTests
{
    [Fact]
    public void IsEnabled_RequiresFlagAndApiKey()
    {
        var disabled = CreateService(new Dictionary<string, string?>
        {
            ["USE_GROK"] = "false",
            ["GROK_API_KEY"] = "xai-key"
        });
        disabled.IsEnabled.Should().BeFalse();

        var missingKey = CreateService(new Dictionary<string, string?>
        {
            ["USE_GROK"] = "true",
            ["GROK_API_KEY"] = ""
        });
        missingKey.IsEnabled.Should().BeFalse();

        var enabled = CreateService(new Dictionary<string, string?>
        {
            ["USE_GROK"] = "true",
            ["GROK_API_KEY"] = "xai-key"
        });
        enabled.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAsync_ReturnsNullWhenDisabled()
    {
        var sut = CreateService(new Dictionary<string, string?> { ["USE_GROK"] = "false" });
        (await sut.CompleteAsync("hello")).Should().BeNull();
    }

    [Fact]
    public async Task CompleteAsync_ParsesSuccessfulResponse()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "choices": [
                        { "message": { "role": "assistant", "content": "Grok answer" } }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var sut = CreateService(new Dictionary<string, string?>
        {
            ["USE_GROK"] = "true",
            ["GROK_API_KEY"] = "xai-key"
        }, handler);

        (await sut.CompleteAsync("What is TABOR?")).Should().Be("Grok answer");
    }

    private static GrokService CreateService(
        Dictionary<string, string?> settings,
        HttpMessageHandler? handler = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings!).Build();
        var http = new HttpClient(handler ?? new HttpClientHandler());
        return new GrokService(http, config, NullLogger<GrokService>.Instance);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
