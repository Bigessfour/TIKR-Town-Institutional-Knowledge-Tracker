using System.Net;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class SettingsPageTests : ClerkTestContext
{
    // ThemeService.InitializeAsync and SetThemeAsync are exercised when rendering Settings (includes TikrThemeSelector)
    // which calls OnAfterRenderAsync -> InitializeAsync and change handlers -> SetThemeAsync.
    // Proof reference for function inventory (bUnit + JS interop defaulted in test context).

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
                "/api/system/document-sdk-status" =>
                    """
                    {"licenseKeyConfigured":true,"licenseProbePassed":true,"licenseProbeDetail":null,"agentToolsEnabled":true,"orchestrationEnabled":false}
                    """,
                "/api/library/scan-status" =>
                    """
                    {"configured":false,"libraryPath":null,"intervalSeconds":300,"pollerActive":false,"lastResult":null,"lastScanUtc":null}
                    """,
                "/api/audit" => "[]",
                "/api/auth/me/tour" => """{"completedVersion":null,"autoTourDisabled":false}""",
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
        cut.Markup.Should().Contain("Syncfusion Document SDK");
        cut.Markup.Should().Contain("Clerk preferences");
        cut.Markup.Should().Contain("docker/.env");
        cut.Markup.Should().Contain("/assistant");
        cut.Markup.Should().Contain("NAS document library");
        cut.Markup.Should().Contain("Scan library now");
        cut.Markup.Should().Contain("Reindex embeddings");
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
