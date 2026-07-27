using Bunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Syncfusion.Blazor;
using Syncfusion.Blazor.AI;
using Syncfusion.Blazor.SmartComponents;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

internal sealed class ClerkTestWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "src", "TIKR.Web", "wwwroot"));

    public string ApplicationName { get; set; } = "TIKR.Web.Tests";
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string EnvironmentName { get; set; } = "Test";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}

public abstract class ClerkTestContext : TestContext
{
    protected ClerkTestContext()
    {
        Services.AddLogging();
        Services.AddSyncfusionBlazor();
        Services.AddSyncfusionSmartComponents().InjectOpenAIInference();
        Services.AddChatClient(_ => new FakeChatClient());
        Services.AddSingleton<IChatInferenceService, SyncfusionAIService>();
        Services.AddSingleton(new SyncfusionBlazorLicenseStatus
        {
            KeyConfigured = true,
            BlazorLicenseValid = true,
            Detail = "Valid for Blazor UI (test host).",
        });
        Services.AddScoped<ClerkToastService>();
        Services.AddSingleton<LocalConnectionStateService>();
        Services.AddScoped<ThemeService>();
        Services.AddSingleton<IWebHostEnvironment>(new ClerkTestWebHostEnvironment());
        Services.AddScoped<ClerkUserGuideService>();
        Services.AddScoped<ClerkTourService>();
        Services.AddSingleton(new AuthSettings { IsEnabled = false });
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("tikrTour.run");
        JSInterop.Setup<bool>("tikrTour.getLocalFlag").SetResult(false);
        JSInterop.Setup<string?>("tikrTour.getLocalValue").SetResult(null);
    }
}

/// <summary>Minimal IChatClient for bUnit (Smart components + Calendar NL).</summary>
internal sealed class FakeChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("fake");

    public void Dispose()
    {
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var json =
            """{"title":"Test deadline","dueDate":"2026-12-31","recurrence":"Annual","description":"from test"}""";
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        EmptyAsync();

    private static async IAsyncEnumerable<ChatResponseUpdate> EmptyAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
