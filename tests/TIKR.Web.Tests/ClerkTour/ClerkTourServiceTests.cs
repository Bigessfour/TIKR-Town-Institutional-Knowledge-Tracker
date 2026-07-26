using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TIKR.Shared.DTOs;
using TIKR.Web.ClerkTour;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.ClerkTour;

public sealed class ClerkTourServiceTests : TestContext
{
    private readonly NavigationManager _navigation;

    public ClerkTourServiceTests()
    {
        _navigation = Services.GetRequiredService<NavigationManager>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("tikrTour.run");
        JSInterop.SetupVoid("tikrTour.setLocalFlag");
        JSInterop.SetupVoid("tikrTour.setLocalValue");
        JSInterop.Setup<bool>("tikrTour.getLocalFlag").SetResult(false);
        JSInterop.Setup<string?>("tikrTour.getLocalValue").SetResult(null);
    }

    [Fact]
    public async Task ShouldAutoStartAsync_ReturnsFalseWhenAutoTourDisabledLocally()
    {
        JSInterop.Setup<bool>("tikrTour.getLocalFlag", ClerkTourService.LocalAutoDisabledKey).SetResult(true);
        var sut = CreateSut(authEnabled: false);

        (await sut.ShouldAutoStartAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldAutoStartAsync_ReturnsTrueWhenVersionNotCompleted()
    {
        var sut = CreateSut(authEnabled: false);

        (await sut.ShouldAutoStartAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task SetAutoTourDisabledAsync_WritesLocalFlagWhenAuthOff()
    {
        var sut = CreateSut(authEnabled: false);

        await sut.SetAutoTourDisabledAsync(true);

        JSInterop.VerifyInvoke("tikrTour.setLocalFlag");
    }

    [Fact]
    public async Task MarkCompletedAsync_WritesLocalVersionWhenAuthOff()
    {
        var sut = CreateSut(authEnabled: false);

        await sut.MarkCompletedAsync();

        JSInterop.VerifyInvoke("tikrTour.setLocalValue");
    }

    [Fact]
    public async Task StartPageTourAsync_InvokesJsRunWithPageSteps()
    {
        var sut = CreateSut(authEnabled: false);

        await sut.StartPageTourAsync("/requirements");

        JSInterop.VerifyInvoke("tikrTour.run");
    }

    [Fact]
    public async Task StartGlobalTourAsync_AndStartFullTourAsync_InvokeJsRun()
    {
        var sut = CreateSut(authEnabled: false);

        await sut.StartGlobalTourAsync();
        JSInterop.VerifyInvoke("tikrTour.run");

        var callbacks = new ClerkTourService.ClerkTourJsCallbacks(sut, _navigation);
        await callbacks.OnTourFinishedFromJs();

        await sut.StartFullTourAsync();
        JSInterop.Invocations.Count(i => i.Identifier == "tikrTour.run").Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task IsAutoTourDisabledForUiAsync_UsesServerStateWhenAuthOn()
    {
        var sut = CreateSut(authEnabled: true, tourState: new ClerkTourStateDto("v1", AutoTourDisabled: true));

        (await sut.IsAutoTourDisabledForUiAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task SetAutoTourDisabledAsync_UsesServerWhenAuthOn()
    {
        ClerkTourStateDto? updated = null;
        var sut = CreateSut(
            authEnabled: true,
            tourState: new ClerkTourStateDto(null, AutoTourDisabled: false),
            onTourUpdate: s => updated = s);

        await sut.SetAutoTourDisabledAsync(true);

        updated!.AutoTourDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task MarkCompletedAsync_UsesServerWhenAuthOn()
    {
        ClerkTourStateDto? updated = null;
        var sut = CreateSut(
            authEnabled: true,
            tourState: new ClerkTourStateDto(null, AutoTourDisabled: false),
            onTourUpdate: s => updated = s);

        await sut.MarkCompletedAsync();

        updated!.CompletedVersion.Should().Be(ClerkTourCatalog.CurrentVersion);
    }

    [Fact]
    public async Task NavigateForTourAsync_NavigatesWhenRouteDiffers()
    {
        var sut = CreateSut(authEnabled: false);
        var callbacks = new ClerkTourService.ClerkTourJsCallbacks(sut, _navigation);

        await callbacks.NavigateForTourAsync("/documents");

        _navigation.Uri.Should().Contain("/documents");
    }

    [Fact]
    public async Task OnTourFinishedFromJs_ClearsRunningAndMarksCompleted()
    {
        var sut = CreateSut(authEnabled: false);
        await sut.StartGlobalTourAsync();
        sut.IsRunning.Should().BeTrue();

        var callbacks = new ClerkTourService.ClerkTourJsCallbacks(sut, _navigation);
        await callbacks.OnTourFinishedFromJs();

        sut.IsRunning.Should().BeFalse();
        JSInterop.VerifyInvoke("tikrTour.setLocalValue");
    }

    private ClerkTourService CreateSut(
        bool authEnabled,
        ClerkTourStateDto? tourState = null,
        Action<ClerkTourStateDto>? onTourUpdate = null)
    {
        var current = tourState ?? new ClerkTourStateDto(null, false);
        var handler = new TourApiHandler(current, onTourUpdate);
        var api = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        var auth = new AuthSettings { IsEnabled = authEnabled };
        return new ClerkTourService(JSInterop.JSRuntime, _navigation, auth, api);
    }

    private sealed class TourApiHandler(ClerkTourStateDto state, Action<ClerkTourStateDto>? onUpdate) : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/auth/me/tour")
            {
                return Task.FromResult(Json(state));
            }

            if (request.Method == HttpMethod.Put && request.RequestUri!.AbsolutePath == "/api/auth/me/tour")
            {
                var body = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                var requestDto = JsonSerializer.Deserialize<UpdateClerkTourStateRequest>(body, JsonOptions)!;
                var updated = new ClerkTourStateDto(
                    requestDto.CompletedVersion ?? state.CompletedVersion,
                    requestDto.AutoTourDisabled ?? state.AutoTourDisabled);
                onUpdate?.Invoke(updated);
                return Task.FromResult(Json(updated));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage Json(ClerkTourStateDto dto) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(dto, JsonOptions), Encoding.UTF8, "application/json")
            };
    }
}
