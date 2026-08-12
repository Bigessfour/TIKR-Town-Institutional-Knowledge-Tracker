using System.Net;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

// Account.razor — clerk password change surface; proof for function inventory surfaces allowlist.

public class AccountPageTests : TestContext
{
    public AccountPageTests()
    {
        Services.AddSyncfusionBlazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddTestAuthorization().SetAuthorized("clerk@test.gov");
    }

    [Fact]
    public void Account_RendersChangePasswordForm()
    {
        RegisterApi();
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Account>();
        cut.Markup.Should().Contain("Account");
        cut.Markup.Should().Contain("Change your password");
        cut.Markup.Should().Contain("Update password");
        cut.FindAll("input[type='password']").Should().HaveCount(3);
    }

    // Write path (/api/auth/change-password) proven in AuthEndpointTests.ChangePassword_WithToken_UpdatesPassword.

    private void RegisterApi()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        }));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }
}
