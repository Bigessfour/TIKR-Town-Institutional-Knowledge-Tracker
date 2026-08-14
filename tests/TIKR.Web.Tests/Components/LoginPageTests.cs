using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Shared.DTOs;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class LoginPageTests : TestContext
{
    public LoginPageTests()
    {
        Services.AddLogging();
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

    [Fact]
    public async Task Login_Submit_SetsErrorWhenCredentialsInvalid()
    {
        Services.AddSingleton(new AuthSettings { IsEnabled = true });
        Services.AddScoped<IAuthSessionService>(_ => new FakeAuthSessionService());
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Login>();
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var model = cut.Instance.GetType().GetField("_model", flags)!.GetValue(cut.Instance)!;
        model.GetType().GetProperty("Email")!.SetValue(model, "clerk@test.gov");
        model.GetType().GetProperty("Password")!.SetValue(model, "wrong");

        var method = cut.Instance.GetType().GetMethod("HandleLoginAsync", flags)!;
        await cut.InvokeAsync(async () =>
        {
            var task = (Task)method.Invoke(cut.Instance, null)!;
            await task;
        });

        var error = cut.Instance.GetType().GetField("_error", flags)!.GetValue(cut.Instance) as string;
        error.Should().Contain("Invalid email or password");
    }

    private sealed class FakeAuthSessionService : IAuthSessionService
    {
        public Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<LoginResponse?>(null);

        public Task LogoutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public string? GetAccessToken() => null;
    }
}
