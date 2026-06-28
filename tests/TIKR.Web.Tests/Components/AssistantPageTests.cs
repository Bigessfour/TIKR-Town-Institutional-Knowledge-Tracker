using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Shared.DTOs;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class AssistantPageTests : TestContext
{
    public AssistantPageTests()
    {
        Services.AddSyncfusionBlazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IChatClient>(new StubChatClient());
        Services.AddSingleton(new ColoradoResourceCatalog([
            new ColoradoResource("CML", "https://www.cml.org", "organization", ["gov"], "Colorado Municipal League")
        ], "2026-06-28"));
    }

    [Fact]
    public async Task Assistant_ShowsUnavailableMessageWhenAskAdvancedFails()
    {
        var handler = new StubHandler((req, _) =>
        {
            var path = req.RequestUri!.PathAndQuery;
            if (path.Contains("ask-advanced", StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"ollamaAvailable":true,"ollamaModel":"llama3.2:3b","grokEnabled":false}""",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));

        var cut = RenderComponent<Assistant>();
        cut.Instance.GetType().GetField("_lastPrompt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(cut.Instance, "What is mill levy?");
        var advancedButton = cut.FindAll("button")
            .Single(b => b.TextContent.Contains("Ask Advanced AI", StringComparison.Ordinal));
        await cut.InvokeAsync(() => advancedButton.Click());

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Advanced AI unavailable"));
    }

    [Fact]
    public async Task Assistant_ShowsAdvancedAiNoteWhenNoPriorPrompt()
    {
        RegisterApi();
        var cut = RenderComponent<Assistant>();

        var advancedButton = cut.FindAll("button")
            .Single(b => b.TextContent.Contains("Ask Advanced AI", StringComparison.Ordinal));
        await cut.InvokeAsync(() => advancedButton.Click());
        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Send a message in the chat first, then click Ask Advanced AI"));
    }

    [Fact]
    public void Assistant_ShowsContextUnavailableWhenPrioritiesFail()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));

        var cut = RenderComponent<Assistant>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Unable to load deadline context from API"));
    }

    [Fact]
    public void Assistant_LoadsDeadlineContextFromApi()
    {
        var prioritiesJson = JsonSerializer.Serialize(new List<DashboardPriority>
        {
            new("Budget due", "Submit soon", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), "High")
        });
        RegisterApi(prioritiesJson);

        var cut = RenderComponent<Assistant>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Budget due"));
    }

    private void RegisterApi(string? prioritiesJson = null)
    {
        prioritiesJson ??= "[]";
        var handler = new StubHandler((req, _) =>
        {
            var path = req.RequestUri!.PathAndQuery;
            var json = path.Contains("dashboard-priorities", StringComparison.Ordinal)
                ? prioritiesJson
                : """{"ollamaAvailable":true,"ollamaModel":"llama3.2:3b","grokEnabled":false}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
    }

    private sealed class StubChatClient : IChatClient
    {
        public ChatClientMetadata Metadata => new("stub");

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "stub")));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            async IAsyncEnumerable<ChatResponseUpdate> Stream()
            {
                await Task.Yield();
                yield return new ChatResponseUpdate(ChatRole.Assistant, "stub stream");
            }

            return Stream();
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }
}
