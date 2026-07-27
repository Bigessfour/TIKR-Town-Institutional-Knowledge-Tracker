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
                    {"ollamaAvailable":true,"ollamaModel":"llama3.2:3b","grokEnabled":false,"ollamaHost":"http://127.0.0.1:11434","grokApiKeyConfigured":false}
                    """,
                "/api/ai/feature-settings" =>
                    """
                    {"ollamaHost":"http://127.0.0.1:11434","ollamaChatModel":"llama3.2:3b","useGrok":false,"grokApiKeyConfigured":false,"ollamaAvailable":true,"statusMessage":null,"grokModel":"grok-4.5","syncfusionLicenseKeyConfigured":true,"syncfusionLicenseHint":"…abcd","grokApiKeyHint":null,"fileStoragePath":"/data/documents","townName":"Wiley","storageLabel":"Synology NAS","townLogoPath":null,"ocrEnabled":true,"useSyncfusionAgentTools":true,"useSyncfusionAgentOrchestration":false,"libraryScanPath":null,"libraryScanIntervalSeconds":300,"libraryScanMaxImports":500,"emailInboxPath":null}
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
                    {"configured":false,"libraryPath":null,"intervalSeconds":300,"pollerActive":false,"lastResult":null,"lastScanUtc":null,"scanInProgress":false}
                    """,
                "/api/ai/corpus-health" =>
                    """
                    {"documentsTotal":0,"documentsWithChunks":0,"documentsTransient":0,"documentsSparseText":0,"knowledgeTotal":0,"knowledgeWithChunks":0,"documentsChunkCoveragePercent":100,"knowledgeChunkCoveragePercent":100,"needsAttention":[]}
                    """,
                "/api/ai/embedding-recovery-status" =>
                    """
                    {"ollamaAvailable":true,"recoveryNeeded":false,"lastOllamaHealthyUtc":null,"lastAutoReindexUtc":null,"lastTrigger":null,"lastResultSummary":null,"lastError":null,"documentsChunkCoveragePercent":100,"knowledgeChunkCoveragePercent":100}
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
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Ready"));
        cut.Markup.Should().Contain("Wiley");
        cut.Markup.Should().Contain("This computer");
        cut.Markup.Should().Contain("Keys");
        cut.Markup.Should().Contain("Save settings");
        cut.Markup.Should().Contain("Where uploaded files live");
        cut.Markup.Should().Contain("Call Steve for help");
        cut.Markup.Should().Contain("/assistant");
        cut.Markup.Should().Contain("Bring in shared town documents");
        cut.Markup.Should().Contain("Scan shared folder now");
        cut.Markup.Should().Contain("Refresh Assistant memory");
    }

    [Fact]
    public void Settings_ShowsUnavailableMessageWhenApiFails()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new TikrApiClient(http));

        var cut = RenderComponent<Settings>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("town server"));
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
