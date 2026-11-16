using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

public class CognitoGateCoverageTests
{
    [Fact]
    public void ClientIdAndDomain_UseConfiguredValuesAndDefaults()
    {
        CognitoSilentAuth.ClientId(Config()).Should().Be(CognitoSilentAuth.DefaultClientId);
        CognitoSilentAuth.ClientId(Config(("TIKR_COGNITO_CLIENT_ID", "  abc  "))).Should().Be("abc");
        CognitoSilentAuth.HostedUiDomain(Config(("TIKR_COGNITO_DOMAIN", "https://auth.example/"))).Should().Be("https://auth.example");
        CognitoIdTokenValidator.PoolId(Config()).Should().Be(CognitoIdTokenValidator.DefaultPoolId);
        CognitoIdTokenValidator.Region(Config(("TIKR_COGNITO_REGION", " us-west-2 "))).Should().Be("us-west-2");
        CognitoIdTokenValidator.Issuer(Config()).Should().Contain(CognitoIdTokenValidator.DefaultPoolId);
    }

    [Theory]
    [InlineData("POST", "/assistant", "text/html", false)]
    [InlineData("GET", "/auth/cognito-start", "text/html", false)]
    [InlineData("GET", "/app.css", "text/html", false)]
    [InlineData("GET", "/assistant", "image/png", false)]
    public void ShouldProbe_SkipsNonDocumentRequests(string method, string path, string accept, bool expected)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        http.Request.Path = path;
        http.Request.Headers.Accept = accept;
        CognitoSilentAuth.ShouldProbe(http.Request, Config(("TIKR_COGNITO_DOMAIN", "auth.example.com"))).Should().Be(expected);
    }

    [Fact]
    public void HasUnexpiredSession_RejectsGarbageAndExpiredTokens()
    {
        var garbage = new DefaultHttpContext();
        garbage.Request.Headers.Cookie = $"{AuthCookie.CognitoSession}=not-a-jwt";
        CognitoSilentAuth.HasUnexpiredSession(garbage.Request).Should().BeFalse();

        var expired = new DefaultHttpContext();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"));
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            expires: DateTime.UtcNow.AddHours(-2),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
        expired.Request.Headers.Cookie = $"{AuthCookie.CognitoSession}={token}";
        CognitoSilentAuth.HasUnexpiredSession(expired.Request).Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "/")]
    [InlineData("  ", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil", "/")]
    [InlineData("/assistant?x=://no", "/")]
    [InlineData("/auth/cognito-start", "/")]
    [InlineData("/vault", "/vault")]
    public void SafeReturnPath_KeepsOnlySameSitePaths(string? candidate, string expected)
    {
        CognitoSilentAuth.SafeReturnPath(candidate).Should().Be(expected);
    }

    [Fact]
    public void AuthorizeAndLogout_AcceptAnAbsoluteHost()
    {
        var url = CognitoSilentAuth.BuildAuthorizeUrl(
            "https://auth.example.com", "client", "https://tikr.example/cb", "state", "challenge", "login");
        url.Should().StartWith("https://auth.example.com/oauth2/authorize?");
        url.Should().Contain("prompt=login");

        CognitoSilentAuth.BuildLogoutUrl("https://auth.example.com/", "client", "https://tikr.example/")
            .Should().StartWith("https://auth.example.com/logout?");
    }

    [Fact]
    public void Pkce_ChallengeMatchesTheVerifier()
    {
        var verifier = CognitoSilentAuth.CreateCodeVerifier();
        var challenge = CognitoSilentAuth.CreateCodeChallenge(verifier);
        verifier.Should().NotContain("=");
        challenge.Should().NotBe(verifier);
        CognitoSilentAuth.CreateState().Should().NotBeNullOrWhiteSpace();
        CognitoSilentAuth.IsSilentRetry("login_required").Should().BeTrue();
        CognitoSilentAuth.IsSilentRetry("access_denied").Should().BeFalse();
    }

    [Fact]
    public void TryReadIdentity_RejectsBadTokensAndKeepsARealEmail()
    {
        CognitoSilentAuth.TryReadIdentity("not-a-jwt", "client", out _, out _).Should().BeFalse();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var wrongAudience = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            audience: "other",
            claims: [new Claim("sub", Guid.NewGuid().ToString())],
            signingCredentials: creds));
        CognitoSilentAuth.TryReadIdentity(wrongAudience, "client", out _, out _).Should().BeFalse();

        var badSub = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            audience: "client",
            claims: [new Claim("sub", "not-a-guid"), new Claim("email", "not-an-email")],
            signingCredentials: creds));
        CognitoSilentAuth.TryReadIdentity(badSub, "client", out _, out _).Should().BeFalse();

        var good = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            audience: "client",
            claims: [new Claim("sub", Guid.NewGuid().ToString()), new Claim("email", " deb@townofwiley.gov ")],
            signingCredentials: creds));
        CognitoSilentAuth.TryReadIdentity(good, "client", out var sub, out var email).Should().BeTrue();
        sub.Should().NotBe(Guid.Empty);
        email.Should().Be("deb@townofwiley.gov");
    }

    [Fact]
    public async Task ExchangeCode_ReadsTheIdTokenAndTreatsFailureAsNull()
    {
        var ok = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id_token":"header.payload.sig"}""", Encoding.UTF8, "application/json")
        });
        var token = await CognitoSilentAuth.ExchangeCodeAsync(
            new HttpClient(ok), "auth.example.com", "client", "secret", "https://tikr/cb", "code", "verifier", CancellationToken.None);
        token.Should().Be("header.payload.sig");

        var denied = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var missing = await CognitoSilentAuth.ExchangeCodeAsync(
            new HttpClient(denied), "https://auth.example.com", "client", null, "https://tikr/cb", "code", "verifier", CancellationToken.None);
        missing.Should().BeNull();
    }

    [Fact]
    public void SessionCookie_IsHttpOnlyAndFollowsHttps()
    {
        var http = new DefaultHttpContext();
        http.Request.Scheme = "https";
        http.Request.Host = new HostString("tikr.example");
        var cookie = CognitoSilentAuth.SessionCookie(http.Request);
        cookie.HttpOnly.Should().BeTrue();
        cookie.Secure.Should().BeTrue();
        cookie.Path.Should().Be("/");
        CognitoSilentAuth.RedirectUri(http.Request).Should().Be("https://tikr.example/auth/cognito-callback");
        CognitoSilentAuth.LogoutUri(http.Request).Should().Be("https://tikr.example/");
    }

    [Fact]
    public void PublicOrigin_UsesHttpsForTheCallbackAndIgnoresPlainHttp()
    {
        var http = new DefaultHttpContext();
        http.Request.Scheme = "http";
        http.Request.Host = new HostString("127.0.0.1", 8080);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [CognitoSilentAuth.PublicOriginConfigKey] = "https://mr-storage.tail57aa13.ts.net:8444/"
        }).Build();

        CognitoSilentAuth.PublicOrigin(config).Should().Be("https://mr-storage.tail57aa13.ts.net:8444");
        CognitoSilentAuth.RedirectUri(http.Request, config)
            .Should().Be("https://mr-storage.tail57aa13.ts.net:8444/auth/cognito-callback");
        CognitoSilentAuth.LogoutUri(http.Request, config)
            .Should().Be("https://mr-storage.tail57aa13.ts.net:8444/");
        CognitoSilentAuth.SessionCookie(http.Request, config).Secure.Should().BeTrue();

        var plain = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [CognitoSilentAuth.PublicOriginConfigKey] = "http://mr-storage:8080"
        }).Build();
        CognitoSilentAuth.PublicOrigin(plain).Should().BeNull();
        CognitoSilentAuth.RedirectUri(http.Request, plain)
            .Should().Be("http://127.0.0.1:8080/auth/cognito-callback");
    }

    [Fact]
    public void CognitoAccess_RejectsAnonymousAndCopiesGroupsOntoRoles()
    {
        CognitoAccess.IsAllowed((ClaimsPrincipal?)null).Should().BeFalse();
        CognitoAccess.IsAllowed(new ClaimsPrincipal(new ClaimsIdentity())).Should().BeFalse();

        var identity = new ClaimsIdentity(
            [new Claim("cognito:groups", "Clerk"), new Claim("email", "deb@townofwiley.gov")],
            authenticationType: "cognito");
        var principal = CognitoAccess.WithGroupRoles(new ClaimsPrincipal(identity));
        principal.IsInRole("Clerk").Should().BeTrue();
        principal.FindFirst(ClaimTypes.Email)!.Value.Should().Be("deb@townofwiley.gov");
    }

    [Fact]
    public void Validate_RejectsAccessTokensAndBadSignatures()
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test-key" };
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        const string issuer = "https://cognito-idp.us-east-2.amazonaws.com/pool";
        var access = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: issuer,
            audience: "client",
            claims: [new Claim("token_use", "access"), new Claim("sub", Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds));

        CognitoIdTokenValidator.Validate(access, issuer, "client", [key]).Should().BeNull();
        CognitoIdTokenValidator.Validate("not-a-jwt", issuer, "client", [key]).Should().BeNull();
        CognitoIdTokenValidator.Validate(access, issuer, "client", []).Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_UsesJwksAndRejectsABlankToken()
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test-key" };
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        var config = Config(
            ("TIKR_COGNITO_USER_POOL_ID", "pool"),
            ("TIKR_COGNITO_REGION", "us-east-2"),
            ("TIKR_COGNITO_CLIENT_ID", "client"));
        var issuer = CognitoIdTokenValidator.Issuer(config);
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: issuer,
            audience: "client",
            claims:
            [
                new Claim("token_use", "id"),
                new Claim("email", "deb@townofwiley.gov"),
                new Claim("cognito:groups", "Staff"),
                new Claim("sub", Guid.NewGuid().ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds));

        var parameters = rsa.ExportParameters(false);
        var jwks = JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    kid = "test-key",
                    alg = "RS256",
                    n = Base64UrlEncoder.Encode(parameters.Modulus),
                    e = Base64UrlEncoder.Encode(parameters.Exponent)
                }
            }
        });
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jwks, Encoding.UTF8, "application/json")
        });
        var validator = new CognitoIdTokenValidator(new StubFactory(handler), config);

        (await validator.ValidateAsync(null)).Should().BeNull();
        var principal = await validator.ValidateAsync(token);
        principal.Should().NotBeNull();
        CognitoAccess.IsAllowed(principal).Should().BeTrue();
        (await validator.ValidateAsync(token)).Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_KeepsGoingWhenJwksCannotBeLoaded()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("offline"));
        var validator = new CognitoIdTokenValidator(new StubFactory(handler), Config());
        (await validator.ValidateAsync("still.a.jwt")).Should().BeNull();
    }

    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => pair.Value))
            .Build();

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
