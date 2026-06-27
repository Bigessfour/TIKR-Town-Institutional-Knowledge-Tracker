using System.Net;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Shared.DTOs;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class SettingsPageTests : TestContext
{
    public SettingsPageTests()
    {
        Services.AddSyncfusionBlazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Settings_RendersOllamaStatusWhenApiResponds()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {"ollamaAvailable":true,"ollamaModel":"llama3.2:3b","grokEnabled":false}
                """,
                System.Text.Encoding.UTF8,
                "application/json")
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new TikrApiClient(http));

        var cut = RenderComponent<Settings>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Connected"));
        cut.Markup.Should().Contain("llama3.2:3b");
        cut.Markup.Should().Contain("/assistant");
    }

    [Fact]
    public void Settings_ShowsUnavailableMessageWhenApiFails()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
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
