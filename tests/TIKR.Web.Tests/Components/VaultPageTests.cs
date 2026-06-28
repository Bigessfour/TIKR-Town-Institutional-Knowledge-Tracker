using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class VaultPageTests : ClerkTestContext
{
    [Fact]
    public void Vault_ShowsEmergencyBanner()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Vault>();
        cut.Markup.Should().Contain("hit by a bus");
        cut.Markup.Should().Contain("Copy Everything for New Clerk");
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
