using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Shared.DTOs;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class HomePageTests : TestContext
{
    public HomePageTests()
    {
        Services.AddSyncfusionBlazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Home_RendersPriorityCardsFromApi()
    {
        var json = JsonSerializer.Serialize(new List<DashboardPriority>
        {
            new("Overdue budget", "Past due", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), "Overdue"),
            new("Soon audit", "Coming up", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), "High")
        });
        RegisterApi(json);

        var cut = RenderComponent<Home>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Overdue budget"));
        cut.Markup.Should().Contain("priority-overdue");
        cut.Markup.Should().Contain("priority-high");
    }

    [Fact]
    public void Home_AppliesMediumPriorityCssClass()
    {
        var json = JsonSerializer.Serialize(new List<DashboardPriority>
        {
            new("Routine filing", "Due later", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)), "Medium")
        });
        RegisterApi(json);

        var cut = RenderComponent<Home>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("priority-medium"));
    }

    [Fact]
    public void Home_HandlesApiFailureGracefully()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));

        var cut = RenderComponent<Home>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("No priorities found"));
    }

    [Fact]
    public void Home_ShowsEmptyStateWhenApiReturnsEmpty()
    {
        RegisterApi("[]");

        var cut = RenderComponent<Home>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("No priorities found"));
    }

    private void RegisterApi(string json)
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
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
