using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Shared.DTOs;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class HomePageTests : ClerkTestContext
{
    public HomePageTests()
    {
        JSInterop.Setup<string?>("tikrDashboardLayout.get", _ => true).SetResult(null);
        JSInterop.SetupVoid("tikrDashboardLayout.set", _ => true);
        JSInterop.SetupVoid("tikrDashboardLayout.remove", _ => true);
    }

    [Fact]
    public void Home_RendersDueOutGridFromSummary()
    {
        RegisterApi(new DashboardSummaryDto(
            1, 2, 1, 0, 0,
            [
                new DashboardDueOutDto(
                    Guid.NewGuid(),
                    "Sales Tax Filing",
                    "Quarterly",
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                    "High",
                    "DOR",
                    null,
                    null,
                    null,
                    false,
                    1,
                    [new RequirementLinkedDocumentDto(Guid.NewGuid(), "sales-tax.pdf", null)])
            ]));

        SetRendererInfo(new RendererInfo("Server", true));
        var cut = RenderComponent<Home>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("e-dashboardlayout"));
        cut.Markup.Should().Contain("due-out-grid");
        cut.Markup.Should().Contain("urgency-strip");
    }

    [Fact]
    public void Home_ShowsResetLayoutButton()
    {
        RegisterApi(EmptySummary());
        SetRendererInfo(new RendererInfo("Server", true));
        var cut = RenderComponent<Home>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Reset layout"));
    }

    [Fact]
    public void Home_HandlesApiFailureGracefully()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Home>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Could not load dashboard"));
    }

    [Fact]
    public void Home_ShowsEmptyDueOutsWhenSummaryEmpty()
    {
        RegisterApi(EmptySummary());
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Home>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("due-out-grid"));
    }

    private static DashboardSummaryDto EmptySummary() =>
        new(0, 0, 0, 0, 0, []);

    private void RegisterApi(DashboardSummaryDto summary)
    {
        var summaryJson = JsonSerializer.Serialize(summary);
        var handler = new StubHandler((request, _) =>
        {
            var path = request.RequestUri?.PathAndQuery ?? "";
            if (path.Contains("/api/dashboard/summary", StringComparison.Ordinal))
                return Json(summaryJson);
            if (path.Contains("/api/audit", StringComparison.Ordinal))
                return Json("[]");
            if (path.Contains("/api/ai/corpus-health", StringComparison.Ordinal))
                return Json("""{"documentsTotal":0,"documentsWithChunks":0,"documentsTransient":0,"documentsSparseText":0,"knowledgeTotal":0,"knowledgeWithChunks":0,"documentsChunkCoveragePercent":100,"knowledgeChunkCoveragePercent":100,"needsAttention":[]}""");
            if (path.Contains("/api/system/local-status", StringComparison.Ordinal))
                return Json("""{"townName":"Wiley","storageLabel":"Synology NAS","dataLastModifiedUtc":null,"ollamaAvailable":true}""");
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }
}
