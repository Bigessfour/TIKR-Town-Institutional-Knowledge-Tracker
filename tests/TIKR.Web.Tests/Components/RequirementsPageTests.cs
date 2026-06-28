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

public class RequirementsPageTests : ClerkTestContext
{
    [Fact]
    public void Requirements_LoadsGridWithSeededData()
    {
        var json = JsonSerializer.Serialize(new List<RequirementDto>
        {
            new(Guid.NewGuid(), "Mill Levy Certification", "Certify levy", new DateOnly(2026, 12, 15),
                RecurrenceType.Annual, RequirementCategory.MillLevy, true, false)
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
    public void Requirements_UsesSfDatePickerWhenDialogOpen()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Requirements>();
        var addButton = cut.FindAll("button")
            .First(b => b.TextContent?.Contains("Add requirement", StringComparison.Ordinal) == true);
        addButton.Click();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("e-datepicker"));
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
