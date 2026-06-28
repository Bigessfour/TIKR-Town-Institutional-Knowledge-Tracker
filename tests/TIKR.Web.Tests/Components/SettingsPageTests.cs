using System.Net;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class SettingsPageTests : ClerkTestContext
{
    [Fact]
    public void Settings_RendersOllamaStatusWhenApiResponds()
    {
        var handler = new StubHandler((req, _) =>
        {
            var path = req.RequestUri!.AbsolutePath;
            var json = path switch
            {
                "/api/ai/status" =>
                    """
                    {"ollamaAvailable":true,"ollamaModel":"llama3.2:3b","grokEnabled":false}
                    """,
                "/api/system/local-status" =>
                    """
                    {"townName":"Wiley","storageLabel":"Synology NAS","dataLastModifiedUtc":"2026-06-28T11:48:00Z","ollamaAvailable":true}
                    """,
                "/api/audit" => "[]",
                _ => "[]"
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new TikrApiClient(http));

        var cut = RenderComponent<Settings>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Connected"));
        cut.Markup.Should().Contain("llama3.2:3b");
        cut.Markup.Should().Contain("Wiley");
        cut.Markup.Should().Contain("/assistant");
    }

    [Fact]
    public void Settings_ShowsUnavailableMessageWhenApiFails()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new TikrApiClient(http));

        var cut = RenderComponent<Settings>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Unable to reach API"));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }
}
