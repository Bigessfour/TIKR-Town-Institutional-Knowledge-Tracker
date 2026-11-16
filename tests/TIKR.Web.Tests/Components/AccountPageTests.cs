using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class AccountPageTests : TestContext
{
    public AccountPageTests()
    {
        Services.AddLogging();
        Services.AddSyncfusionBlazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddTestAuthorization();
    }

    [Fact]
    public void Account_SaysSignInIsOffWhenCognitoIsNotConfigured()
    {
        Services.AddSingleton(new AuthSettings { IsEnabled = false, CognitoGateEnabled = false });
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Account>();

        cut.Markup.Should().Contain("Sign-in is turned off");
        cut.Markup.Should().NotContain("Update password");
    }

    [Fact]
    public void Account_ShowsTownSignInAndSignOut()
    {
        Services.AddSingleton(new AuthSettings { IsEnabled = false, CognitoGateEnabled = true });
        this.AddTestAuthorization().SetAuthorized("clerk@townofwiley.gov", AuthorizationState.Authorized);
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Account>();

        cut.Markup.Should().Contain("clerk@townofwiley.gov");
        cut.Markup.Should().Contain("Sign out");
        cut.Markup.Should().Contain("/auth/cognito-logout");
    }
}
