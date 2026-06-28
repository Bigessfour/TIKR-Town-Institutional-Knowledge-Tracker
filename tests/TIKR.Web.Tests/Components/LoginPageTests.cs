using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class LoginPageTests : TestContext
{
    public LoginPageTests()
    {
        Services.AddSyncfusionBlazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Login_RendersSignInForm_WhenAuthEnabled()
    {
        Services.AddSingleton(new AuthSettings { IsEnabled = true });
        Services.AddScoped<IAuthSessionService>(_ => new FakeAuthSessionService());
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Login>();
        cut.Markup.Should().Contain("Sign in to TIKR");
        cut.Markup.Should().Contain("Email");
        cut.Markup.Should().Contain("Password");
    }

    [Fact]
    public void Login_ShowsDisabledMessage_WhenAuthOff()
    {
        Services.AddSingleton(new AuthSettings { IsEnabled = false });
        Services.AddScoped<IAuthSessionService>(_ => new FakeAuthSessionService());

        var cut = RenderComponent<Login>();
        cut.Markup.Should().Contain("Authentication is not enabled");
    }

    private sealed class FakeAuthSessionService : IAuthSessionService
    {
        public Task<Shared.DTOs.LoginResponse?> LoginAsync(Shared.DTOs.LoginRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<Shared.DTOs.LoginResponse?>(null);

        public Task LogoutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public string? GetAccessToken() => null;
    }
}
