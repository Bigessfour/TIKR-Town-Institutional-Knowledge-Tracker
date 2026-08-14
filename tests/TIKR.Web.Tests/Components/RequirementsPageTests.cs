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
using TIKR.Shared.TestFixtures;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

[Trait("Category", TestCategories.FullyTested)]
public class RequirementsPageTests : ClerkTestContext
{
    [Fact]
    public void Requirements_LoadsGridWithSeededData()
    {
        var json = JsonSerializer.Serialize(new List<RequirementDto>
        {
            new(Guid.NewGuid(), "Mill Levy Certification", "Certify levy", new DateOnly(2026, 12, 15),
                RecurrenceType.Annual, RequirementCategory.MillLevy, true, false, [])
        });
        RegisterApi(json);
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Requirements>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Mill Levy Certification"));
        cut.Markup.Should().Contain("Requirements Manager");
        cut.Markup.Should().Contain("deputy clerk needs this list");
    }

    [Fact]
    public void Requirements_RendersAgentScanUploadControl()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Requirements>();

        cut.Markup.Should().Contain("AI Scan uploaded doc");
        cut.Markup.Should().Contain("e-upload");
    }

    [Fact]
    public void Requirements_RendersDocumentGenerationActions()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Requirements>();

        cut.Markup.Should().Contain("Council packet");
        cut.Markup.Should().Contain("Download agenda PDF");
        cut.Markup.Should().Contain("Compliance Excel");
        cut.Markup.Should().Contain("Meeting minutes");
    }

    [Fact]
    public async Task Requirements_UsesSfDatePickerWhenDialogOpen()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Requirements>();
        var addButton = cut.FindAll("button")
            .First(b => b.TextContent?.Contains("Add requirement", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => addButton.Click());
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Select due date"));
    }

    [Fact]
    public void Requirements_RendersDeleteActionForNonSeededRows()
    {
        var json = JsonSerializer.Serialize(new List<RequirementDto>
        {
            new(Guid.NewGuid(), "Clerk-added filing", "Local obligation", new DateOnly(2026, 12, 15),
                RecurrenceType.Annual, RequirementCategory.Custom, false, false, [])
        });
        RegisterApi(json);
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Requirements>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Delete"));
    }

    [Fact]
    public async Task Requirements_ShowsContactPickerWhenDialogOpen()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Requirements>();
        var addButton = cut.FindAll("button")
            .First(b => b.TextContent?.Contains("Add requirement", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => addButton.Click());
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Pick inventory contact"));
        cut.Markup.Should().Contain("Apply as primary");
        cut.Markup.Should().Contain("Submit to");
    }

    [Fact]
    public void Requirements_ShowsPlaybookProgressOnGrid()
    {
        var json = JsonSerializer.Serialize(new List<RequirementDto>
        {
            RequirementDtoFactory.Create(
                title: "Election Canvass & Certification",
                category: RequirementCategory.Election,
                isSystemSeeded: true,
                checklistCompleted: 2,
                checklistTotal: 5)
        });
        RegisterApi(json);
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Requirements>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("2/5"));
        cut.Markup.Should().Contain("Playbook");
    }

    [Fact]
    public async Task Requirements_ShowsChecklistPanelWhenEditing()
    {
        var id = Guid.NewGuid();
        var requirement = RequirementDtoFactory.Create(
            id: id,
            title: "Election Canvass & Certification",
            category: RequirementCategory.Election,
            isSystemSeeded: true,
            checklistCompleted: 0,
            checklistTotal: 2);
        var checklist = new List<RequirementChecklistItemDto>
        {
            new(Guid.NewGuid(), id, "Assemble canvass packet", "Gather abstracts", true, false,
                3, null, new DateOnly(2026, 11, 12), 0, null, "canvass-packet.pdf", "County Clerk", null),
            new(Guid.NewGuid(), id, "File certification", null, true, false,
                0, null, new DateOnly(2026, 11, 15), 1, null, null, "SOS", null)
        };

        RegisterApi(JsonSerializer.Serialize(new List<RequirementDto> { requirement }), checklist);
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Requirements>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Election Canvass"));
        var editButton = cut.FindAll("button")
            .First(b => b.TextContent?.Contains("Edit", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => editButton.Click());

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Playbook checklist"));
        cut.Markup.Should().Contain("Assemble canvass packet");
        cut.Markup.Should().Contain("data-tour=\"requirements-checklist\"");
        cut.Markup.Should().Contain("0/2 complete");
        cut.Markup.Should().Contain("Edit step");

        var editStep = cut.FindAll("button")
            .First(b => b.TextContent?.Contains("Edit step", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => editStep.Click());
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("data-tour=\"requirements-checklist-edit\""));
        cut.Markup.Should().Contain("Save step");
    }

    private void RegisterApi(string json, List<RequirementChecklistItemDto>? checklist = null)
    {
        var checklistJson = JsonSerializer.Serialize(checklist ?? []);
        var handler = new StubHandler((req, _) =>
        {
            var path = req.RequestUri!.AbsolutePath;
            var body = path switch
            {
                "/api/system/local-status" =>
                    """
                    {"townName":"Wiley","storageLabel":"Synology NAS","dataLastModifiedUtc":null,"ollamaAvailable":true}
                    """,
                "/api/contacts" => "[]",
                _ when path.Contains("/checklist", StringComparison.Ordinal) => checklistJson,
                _ when path.Contains("/contacts", StringComparison.Ordinal) => "[]",
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
