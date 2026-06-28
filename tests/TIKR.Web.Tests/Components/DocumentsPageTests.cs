using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Shared.DTOs;
using TIKR.Shared.TestFixtures;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentsPageTests : ClerkTestContext
{

    [Fact]
    public void Documents_LoadsDocumentListFromApi()
    {
        var docId = Guid.NewGuid();
        var docsJson = JsonSerializer.Serialize(new List<DocumentDto>
        {
            new(docId, "budget-2026.pdf", "application/pdf", 1024, null, "Finance", DateTime.UtcNow)
        });
        RegisterApi(docsJson);
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Documents>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("budget-2026.pdf"));
        cut.Markup.Should().Contain("Document Library");
    }

    [Fact]
    public void Documents_ShowsSemanticSearchToggle()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Documents>();
        cut.Markup.Should().Contain("Semantic search");
        cut.Markup.Should().Contain("Full-text search");
    }

    private void RegisterApi(string docsJson, string? searchJson = null)
    {
        searchJson ??= JsonSerializer.Serialize(new SemanticSearchResponse("q", 0, []));
        var handler = new StubHandler((req, _) =>
        {
            var path = req.RequestUri!.PathAndQuery;
            var json = path.Contains("semantic-search", StringComparison.Ordinal) ? searchJson : docsJson;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }
}
