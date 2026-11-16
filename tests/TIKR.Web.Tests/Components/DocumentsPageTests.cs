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
    public DocumentsPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

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
        cut.Markup.Should().Contain("Search by meaning");
        cut.Markup.Should().Contain("Search words");
    }

    [Fact]
    public void Documents_RendersPreviewPaneHost()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Documents>();
        cut.Markup.Should().Contain("preview-pane");
        cut.Markup.Should().Contain("Select a document in the list");
        cut.Markup.Should().Contain("Add files");
    }

    [Fact]
    public void Documents_ShowsIntakeBannerWhenBriefPending()
    {
        var docId = Guid.NewGuid();
        var brief = new DocumentIntakeBriefDto(
            Guid.NewGuid(),
            docId,
            "notice.pdf",
            "Pending",
            [new DocumentIntakeDueDateDto(new DateOnly(2027, 6, 1), "Hearing")],
            [new DocumentIntakeContactDto("Casey Clerk")],
            [new DocumentIntakeActionItemDto("Post notice")],
            ProcessedLocally: true,
            UsedGrok: false,
            CreatedAtUtc: DateTime.UtcNow);

        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Documents>();
        var field = typeof(Documents).GetField("_intakeBrief",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.Should().NotBeNull();
        field!.SetValue(cut.Instance, brief);
        cut.Render();

        cut.Markup.Should().Contain("Document intake");
        cut.Markup.Should().Contain("On this NAS only");
        cut.Markup.Should().Contain("Add to Calendar");
        cut.Markup.Should().Contain("Save contacts");
    }

    [Fact]
    public void Documents_ShowsLibraryAndBrowseModeToggle()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Documents>();
        cut.Markup.Should().Contain("Library");
        cut.Markup.Should().Contain("Folders");
        cut.Markup.Should().Contain("Recycle bin");
    }

    [Fact]
    public void Documents_BrowseMode_ShowsFileManager()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Documents>();
        var browseBtn = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Folders", StringComparison.OrdinalIgnoreCase));
        browseBtn.Should().NotBeNull("Folders toggle should render");
        browseBtn!.Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("documents-browse-host");
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Documents_WhenApiFails_ShowsEmptyOrErrorState()
    {
        var handler = new StubHandler((_, _) => throw new HttpRequestException("Connection refused"));
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Documents>();
        cut.WaitForAssertion(() =>
        {
            // Load failure should not leave a blank page — library chrome still present.
            cut.Markup.Should().Contain("Document Library");
        });
    }

    private void RegisterApi(string docsJson, string? searchJson = null, string? intakeBriefJson = null)
    {
        searchJson ??= JsonSerializer.Serialize(new SemanticSearchResponse("q", 0, []));
        var handler = new StubHandler((req, _) =>
        {
            var path = req.RequestUri!.PathAndQuery;
            if (path.Contains("/content", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent("%PDF-1.4"u8.ToArray())
                };
            }

            if (path.Contains("/email/notices", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                };
            }

            if (path.Contains("intake-brief", StringComparison.OrdinalIgnoreCase))
            {
                if (intakeBriefJson is null)
                {
                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(intakeBriefJson, Encoding.UTF8, "application/json")
                };
            }

            if (path.Contains("intake-scan", StringComparison.OrdinalIgnoreCase) && intakeBriefJson is not null)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(intakeBriefJson, Encoding.UTF8, "application/json")
                };
            }

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
