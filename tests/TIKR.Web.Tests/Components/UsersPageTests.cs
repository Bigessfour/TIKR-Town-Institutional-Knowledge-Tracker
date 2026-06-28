using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Shared.Constants;
using TIKR.Shared.DTOs;
using TIKR.Web.Components.Pages;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class UsersPageTests : TestContext
{
    public UsersPageTests()
    {
        Services.AddSyncfusionBlazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddTestAuthorization()
            .SetAuthorized("admin@test.gov", AuthorizationState.Authorized)
            .SetRoles(TikrRoles.Admin);
    }

    [Fact]
    public void Users_RendersGridWithSeededUsers()
    {
        var handler = new StubHandler((request, _) =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/auth/users")
            {
                var users = new List<UserSummaryDto>
                {
                    new("1", "admin@test.gov", "Admin", true, ["Admin"]),
                    new("2", "clerk@test.gov", "Clerk", true, ["Clerk"])
                };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(users)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new TikrApiClient(http));
        SetRendererInfo(new RendererInfo("Server", true));

        var cut = RenderComponent<Users>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("admin@test.gov"));
        cut.Markup.Should().Contain("clerk@test.gov");
        cut.Markup.Should().Contain("Add user");
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
