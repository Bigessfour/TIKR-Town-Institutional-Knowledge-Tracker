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
        cut.Markup.Should().Contain("Generate Complete Handover Package");
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

    [Fact]
    public void Vault_LoadsHowToEntriesFromApi()
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new List<KnowledgeEntryDto>
        {
            new(id, "TD Drive how-to", "<p>Call county first</p>", KnowledgeCategory.HowTo, 1),
            new(Guid.NewGuid(), "Vendor contact", "ACME 555", KnowledgeCategory.Contact, 2),
        });
        RegisterApi(json);
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Vault>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("TD Drive how-to"));
        cut.Markup.Should().Contain("Knowledge Vault");
        cut.Markup.Should().Contain("How-To");
    }

    [Fact]
    public async Task Vault_CopyForNewClerk_InvokesClipboard()
    {
        var json = JsonSerializer.Serialize(new List<KnowledgeEntryDto>
        {
            new(Guid.NewGuid(), "Copy me", "secret process", KnowledgeCategory.HowTo, 1),
        });
        RegisterApi(json);
        JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true);
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Vault>();
        // CI runs Web tests in parallel with other assemblies; async vault load can exceed the
        // default 1s WaitForAssertion window (seen as Check count: 0 / render count: 1).
        cut.WaitForAssertion(
            () =>
            {
                cut.Markup.Should().NotContain("Loading vault entries");
                cut.Markup.Should().Contain("Copy me");
            },
            TimeSpan.FromSeconds(10));

        var copyBtn = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Copy Everything for New Clerk", StringComparison.Ordinal));
        await cut.InvokeAsync(() => copyBtn.Click());

        JSInterop.VerifyInvoke("navigator.clipboard.writeText");
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
