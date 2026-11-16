using System.Net;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Web.Components.Shared;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class TikrStatusFooterTests : ClerkTestContext
{
    [Fact]
    public void StatusFooter_RendersLocalNasMessageWhenApiResponds()
    {
        var json =
            """
            {"townName":"Wiley","storageLabel":"Synology NAS","dataLastModifiedUtc":"2026-06-28T11:48:00Z","ollamaAvailable":true}
            """;
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<TikrStatusFooter>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Town files stay on town storage"));
        cut.Markup.Should().Contain("Helper is ready");
        cut.Markup.Should().Contain("5:48");
        cut.Markup.Should().NotContain("Synology");
        cut.Markup.Should().NotContain("11:48");
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
