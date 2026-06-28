using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class KnowledgePageTests : TestContext
{
    public KnowledgePageTests()
    {
        Services.AddSyncfusionBlazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Knowledge_LoadsEntriesFromApi()
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new List<KnowledgeEntryDto>
        {
            new(id, "How to run elections", "Steps...", KnowledgeCategory.HowTo, 0)
        });
        RegisterApi(json);

        var cut = RenderComponent<Knowledge>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("How to run elections"));
    }

    [Fact]
    public void Knowledge_RendersAddEntryForm()
    {
        RegisterApi("[]");

        var cut = RenderComponent<Knowledge>();
        cut.Markup.Should().Contain("Save Entry");
        cut.Markup.Should().Contain("Add Entry");
    }

    private void RegisterApi(string getJson)
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(getJson, Encoding.UTF8, "application/json")
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
