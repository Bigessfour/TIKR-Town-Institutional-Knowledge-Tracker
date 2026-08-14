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

// Calendar.razor — clerk deadline calendar surface; proof for function inventory surfaces allowlist.

public class CalendarPageTests : ClerkTestContext
{
    public CalendarPageTests()
    {
        Services.AddSyncfusionBlazor();
        Services.AddSingleton(new SyncfusionBlazorLicenseStatus
        {
            KeyConfigured = true,
            BlazorLicenseValid = true,
            Detail = "Valid for Blazor UI (test host).",
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Calendar_LoadsRequirementsIntoGrid()
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new List<RequirementDto>
        {
            new(id, "Mill Levy Certification", "Certify levy", new DateOnly(2026, 12, 15),
                RecurrenceType.Annual, RequirementCategory.MillLevy, true, false, [])
        });
        RegisterApi(json);
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Calendar>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Mill Levy Certification"));
        cut.Markup.Should().Contain("Deadline Calendar");
        cut.Markup.Should().Contain("e-schedule");
        cut.FindComponent<Syncfusion.Blazor.Schedule.SfSchedule<Calendar.CalendarEvent>>()
            .Instance.AllowDragAndDrop.Should().BeTrue();
        cut.FindComponent<Syncfusion.Blazor.Schedule.SfSchedule<Calendar.CalendarEvent>>()
            .Instance.Readonly.Should().BeFalse();
    }

    [Fact]
    public void Calendar_WhenBlazorLicenseInvalid_ShowsMessageAndSkipsSchedule()
    {
        Services.AddSingleton(new SyncfusionBlazorLicenseStatus
        {
            KeyConfigured = true,
            BlazorLicenseValid = false,
            Detail = "The included Syncfusion license key is invalid.",
        });
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Calendar>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Schedule view needs a valid Syncfusion Blazor license"));
        cut.Markup.Should().NotContain("e-schedule");
    }

    [Fact]
    public void Calendar_ShowsContactPickerPanel()
    {
        RegisterApi("[]");
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Calendar>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Apply inventory contact"));
        cut.Markup.Should().Contain("calendar-contact-picker");
    }

    private void RegisterApi(string json)
    {
        var handler = new StubHandler((req, _) =>
        {
            var path = req.RequestUri!.AbsolutePath;
            var body = path == "/api/contacts" ? "[]" : json;
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
