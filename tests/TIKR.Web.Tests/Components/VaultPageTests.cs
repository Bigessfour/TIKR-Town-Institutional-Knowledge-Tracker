using System.Net;
using System.Text;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

// Vault.razor — clerk knowledge vault surface; proof for function inventory surfaces allowlist.

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

    [Fact]
    public async Task Vault_ContactsTab_ShowsInventoryActions()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Vault>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Contacts"));

        var contactsTab = cut.FindAll(".e-tab-text")
            .First(e => e.TextContent?.Contains("Contacts", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => contactsTab.Click());

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Add contact");
            cut.Markup.Should().Contain("vault-contacts-inventory");
        });
    }

    private void RegisterApi(string json)
    {
        var handler = new StubHandler((req, _) =>
        {
            var path = req.RequestUri!.AbsolutePath;
            var body = path switch
            {
                "/api/contacts" => "[]",
                _ => json
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
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
