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
using TIKR.Web.Helpers;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class AssistantPageTests : ClerkTestContext
{
    public AssistantPageTests()
    {
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
    public async Task Assistant_ClearConversation_ResetsModelHistoryAndShowsNote()
    {
        RegisterApi();
        var cut = RenderComponent<Assistant>();

        var historyField = cut.Instance.GetType().GetField(
            "_modelHistory",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var history = (List<ChatMessage>)historyField.GetValue(cut.Instance)!;
        history.Add(new ChatMessage(ChatRole.User, "prior"));
        history.Add(new ChatMessage(ChatRole.Assistant, "answer"));

        cut.Instance.GetType().GetField("_lastPrompt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(cut.Instance, "prior");

        var clearButton = cut.FindAll("button")
            .Single(b => b.TextContent.Contains("Clear conversation", StringComparison.Ordinal));
        await cut.InvokeAsync(() => clearButton.Click());

        history.Should().BeEmpty();
        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Conversation cleared"));
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

    [Fact]
    public async Task Assistant_MainChatPrompt_ReceivesStreamedResponse()
    {
        RegisterApi();
        var cut = RenderComponent<Assistant>();

        var method = cut.Instance.GetType().GetMethod("OnPromptRequested", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var args = new Syncfusion.Blazor.InteractiveChat.AssistViewPromptRequestedEventArgs { Prompt = "What is the budget?" };
        var task = (Task)method.Invoke(cut.Instance, new object[] { args });
        await cut.InvokeAsync(async () => await task);

        // Stub yields "stub stream"; we set args.Response to the Markdown HTML containing it
        args.Response.Should().Contain("stub stream");
    }

    [Fact]
    public void FormatStreamingHtml_EncodesAndPreservesAccumulatedText()
    {
        var html = AssistantPromptBuilder.FormatStreamingHtml("Hello <world> & more");
        html.Should().Contain("tikr-assist-stream");
        html.Should().Contain("Hello &lt;world&gt; &amp; more");
        html.Should().NotContain("<world>");
    }

    [Fact]
    public async Task Assistant_MainChatPrompt_AccumulatesMultiChunkStream()
    {
        // Constructor registers StubChatClient; replace so this test owns the stream chunks.
        var existing = Services.FirstOrDefault(d => d.ServiceType == typeof(IChatClient));
        if (existing is not null)
            Services.Remove(existing);
        Services.AddSingleton<IChatClient>(new MultiChunkStubChatClient());
        RegisterApi();
        var cut = RenderComponent<Assistant>();

        var method = cut.Instance.GetType().GetMethod(
            "OnPromptRequested",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var args = new Syncfusion.Blazor.InteractiveChat.AssistViewPromptRequestedEventArgs { Prompt = "Say hello" };
        var task = (Task)method.Invoke(cut.Instance, [args])!;
        await cut.InvokeAsync(async () => await task);

        // Final response must contain the full concatenated stream, not only the last chunk.
        args.Response.Should().Contain("Hello");
        args.Response.Should().Contain("world");
        args.Response.Should().Contain("from");
        args.Response.Should().Contain("TIKR");
    }

    private void RegisterApi(string? prioritiesJson = null)
    {
        prioritiesJson ??= "[]";
        var handler = new StubHandler((req, _) =>
        {
            var path = req.RequestUri!.PathAndQuery;
            var json = path switch
            {
                _ when path.Contains("dashboard-priorities", StringComparison.Ordinal) => prioritiesJson,
                _ when path.Contains("semantic-search-knowledge", StringComparison.Ordinal) =>
                    """{"query":"","considered":0,"hits":[],"embeddingAvailable":true}""",
                _ when path.Contains("semantic-search", StringComparison.Ordinal) =>
                    """{"query":"","considered":0,"hits":[],"embeddingAvailable":true}""",
                _ => """{"ollamaAvailable":true,"ollamaModel":"llama3.2:3b","grokEnabled":false}"""
            };
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

    /// <summary>Yields several chunks with delays so the UI throttle path runs between updates.</summary>
    private sealed class MultiChunkStubChatClient : IChatClient
    {
        public ChatClientMetadata Metadata => new("multi-chunk-stub");

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello world from TIKR")));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            async IAsyncEnumerable<ChatResponseUpdate> Stream()
            {
                foreach (var part in new[] { "Hello ", "world ", "from ", "TIKR" })
                {
                    await Task.Delay(50, cancellationToken);
                    yield return new ChatResponseUpdate(ChatRole.Assistant, part);
                }
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
