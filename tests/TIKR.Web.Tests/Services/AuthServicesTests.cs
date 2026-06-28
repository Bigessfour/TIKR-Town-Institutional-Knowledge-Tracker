using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TIKR.Shared.Constants;
using TIKR.Shared.DTOs;
using TIKR.Shared.TestFixtures;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

public class AuthServicesTests
{
    [Fact]
    public async Task JwtAuthorizationHandler_AddsBearerHeaderWhenCookiePresent()
    {
        var token = CreateTestJwt();
        var context = CreateHttpContext(token);
        var accessor = new HttpContextAccessor { HttpContext = context };

        string? authorization = null;
        var inner = new CapturingHandler(req =>
        {
            authorization = req.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var handler = new JwtAuthorizationHandler(accessor) { InnerHandler = inner };
        var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/api/requirements");

        authorization.Should().Be($"Bearer {token}");
    }

    [Fact]
    public async Task JwtAuthorizationHandler_SkipsAuthorizationWhenNoCookie()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        string? authorization = null;
        var inner = new CapturingHandler(req =>
        {
            authorization = req.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var handler = new JwtAuthorizationHandler(accessor) { InnerHandler = inner };
        var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/api/requirements");

        authorization.Should().BeNull();
    }

    [Fact]
    public async Task TikrAuthenticationStateProvider_ReturnsAuthenticatedWhenValidJwtCookie()
    {
        var token = CreateTestJwt("clerk@town.gov", TikrRoles.Admin);
        var provider = new TikrAuthenticationStateProvider(new HttpContextAccessor
        {
            HttpContext = CreateHttpContext(token)
        });

        var state = await provider.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeTrue();
        state.User.FindFirst(ClaimTypes.Email)!.Value.Should().Be("clerk@town.gov");
        state.User.IsInRole(TikrRoles.Admin).Should().BeTrue();
    }

    [Fact]
    public async Task TikrAuthenticationStateProvider_ReturnsAnonymousWhenNoCookie()
    {
        var provider = new TikrAuthenticationStateProvider(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });

        var state = await provider.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task TikrAuthenticationStateProvider_ReturnsAnonymousWhenTokenInvalid()
    {
        var provider = new TikrAuthenticationStateProvider(new HttpContextAccessor
        {
            HttpContext = CreateHttpContext("not-a-jwt")
        });

        var state = await provider.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void AuthSessionService_GetAccessToken_ReturnsCookieValue()
    {
        var token = CreateTestJwt();
        var accessor = new HttpContextAccessor { HttpContext = CreateHttpContext(token) };
        var provider = new TikrAuthenticationStateProvider(accessor);
        var sut = new AuthSessionService(new StubHttpClientFactory(), accessor, provider);

        sut.GetAccessToken().Should().Be(token);
    }

    [Fact]
    public async Task AuthSessionService_LoginAsync_ReturnsNullOnFailure()
    {
        var factory = new StubHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var provider = new TikrAuthenticationStateProvider(accessor);
        var sut = new AuthSessionService(factory, accessor, provider);

        var result = await sut.LoginAsync(new LoginRequest("bad@town.gov", "wrong"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthSessionService_LoginAsync_ReturnsResponseOnSuccess()
    {
        var expires = DateTime.UtcNow.AddHours(8);
        var login = new LoginResponse("jwt-token", expires, "clerk@town.gov", [TikrRoles.Clerk]);
        var json = JsonSerializer.Serialize(login);
        var factory = new StubHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var context = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var provider = new TikrAuthenticationStateProvider(accessor);
        var sut = new AuthSessionService(factory, accessor, provider);

        var result = await sut.LoginAsync(new LoginRequest(TestAuthFixtures.ClerkEmail, TestAuthFixtures.BootstrapPassword));

        result.Should().NotBeNull();
        result!.Email.Should().Be("clerk@town.gov");
        context.Response.Headers.SetCookie.ToString().Should().Contain(AuthCookie.Name);
    }

    [Fact]
    public async Task AuthSessionService_LogoutAsync_DeletesCookie()
    {
        var context = CreateHttpContext(CreateTestJwt());
        var accessor = new HttpContextAccessor { HttpContext = context };
        var provider = new TikrAuthenticationStateProvider(accessor);
        var sut = new AuthSessionService(new StubHttpClientFactory(), accessor, provider);

        await sut.LogoutAsync();

        context.Response.Headers.SetCookie.ToString().Should().Contain($"{AuthCookie.Name}=");
    }

    private static string CreateTestJwt(string email = "clerk@test.gov", string role = TikrRoles.Clerk)
    {
        var token = new JwtSecurityToken(claims:
        [
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.NameIdentifier, "user-1")
        ]);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static DefaultHttpContext CreateHttpContext(string token)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{AuthCookie.Name}={token}";
        return context;
    }

    private sealed class StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
        : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new CapturingHandler(req => responder?.Invoke(req) ?? new HttpResponseMessage(HttpStatusCode.OK)))
            {
                BaseAddress = new Uri("http://localhost/")
            };
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
