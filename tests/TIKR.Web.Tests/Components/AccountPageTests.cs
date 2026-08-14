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
        this.AddTestAuthorization()
            .SetAuthorized("clerk@test.gov", AuthorizationState.Authorized);
    }

    [Fact]
    public void Account_RendersChangePasswordForm()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Account>();
        cut.Markup.Should().Contain("Change your password");
        cut.Markup.Should().Contain("Update password");
        cut.Markup.Should().Contain("Account");
    }

    [Fact]
    public async Task Account_ChangePassword_SetsSuccessWhenApiOk()
    {
        var handler = new StubHandler((req, _) =>
        {
            if (req.Method == HttpMethod.Post &&
                req.RequestUri!.AbsolutePath.Contains("password", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });
        Services.AddSingleton(new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Account>();
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var model = cut.Instance.GetType().GetField("_model", flags)!.GetValue(cut.Instance)!;
        model.GetType().GetProperty("CurrentPassword")!.SetValue(model, "old-pass");
        model.GetType().GetProperty("NewPassword")!.SetValue(model, "new-pass-123");
        model.GetType().GetProperty("ConfirmPassword")!.SetValue(model, "new-pass-123");

        var method = cut.Instance.GetType().GetMethod("HandleChangePasswordAsync", flags)!;
        await cut.InvokeAsync(async () =>
        {
            var task = (Task)method.Invoke(cut.Instance, null)!;
            await task;
        });

        var message = cut.Instance.GetType().GetField("_message", flags)!.GetValue(cut.Instance) as string;
        message.Should().Contain("Password updated successfully");
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }
}
