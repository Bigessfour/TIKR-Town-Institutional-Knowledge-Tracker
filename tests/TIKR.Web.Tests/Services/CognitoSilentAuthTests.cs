using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

public class CognitoSilentAuthTests
{
    [Fact]
    public void ShouldProbe_IsFalseWhenDomainMissing_SoItDoesNotRedirectForever()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Get;
        http.Request.Path = "/assistant";
        http.Request.Headers.Accept = "text/html";

        CognitoSilentAuth.ShouldProbe(http.Request, Config(domain: null)).Should().BeFalse();
    }

    [Fact]
    public void ShouldProbe_IsFalseWhenSessionCookieIsStillValid()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Get;
        http.Request.Path = "/assistant";
        http.Request.Headers.Accept = "text/html";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"));
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
        http.Request.Headers.Cookie = $"{AuthCookie.CognitoSession}={token}";

        CognitoSilentAuth.ShouldProbe(http.Request, Config("auth.example.com")).Should().BeFalse();
    }

    [Fact]
    public void ShouldProbe_RunsAgainWhenOnlyTheOldSkipCookieIsPresent()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Get;
        http.Request.Path = "/assistant";
        http.Request.Headers.Accept = "text/html";
        http.Request.Headers.Cookie = $"{AuthCookie.CognitoChecked}=1";

        CognitoSilentAuth.ShouldProbe(http.Request, Config("auth.example.com")).Should().BeTrue();
    }

    [Fact]
    public void BuildAuthorizeUrl_OmitsPromptOnInteractiveLogin()
    {
        var silent = CognitoSilentAuth.BuildAuthorizeUrl(
            "auth.example.com", "client", "https://tikr.example/auth/cognito-callback", "state", "challenge");
        var interactive = CognitoSilentAuth.BuildAuthorizeUrl(
            "auth.example.com", "client", "https://tikr.example/auth/cognito-callback", "state", "challenge", prompt: null);

        silent.Should().Contain("prompt=none");
        interactive.Should().NotContain("prompt=");
    }

    [Fact]
    public void BuildLogoutUrl_TargetsHostedUiLogout()
    {
        var url = CognitoSilentAuth.BuildLogoutUrl(
            "auth.example.com", "client", "https://tikr.example/");
        url.Should().StartWith("https://auth.example.com/logout?");
        url.Should().Contain("client_id=client");
        url.Should().Contain("logout_uri=");
    }

    [Theory]
    [MemberData(nameof(GroupCases))]
    public void CognitoAccess_AllowsStaffAndRejectsDriverOnly(string[] groups, bool allowed)
    {
        CognitoAccess.IsAllowed(groups).Should().Be(allowed);
    }

    public static IEnumerable<object[]> GroupCases()
    {
        yield return [Array.Empty<string>(), true];
        yield return [new[] { "Staff" }, true];
        yield return [new[] { "admin" }, true];
        yield return [new[] { "Driver", "Staff" }, true];
        yield return [new[] { "Driver" }, false];
    }

    [Fact]
    public void Validate_AcceptsStaffIdTokenAndRejectsDriver()
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test-key" };
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        const string issuer = "https://cognito-idp.us-east-2.amazonaws.com/us-east-2_bHk9UcenK";
        const string audience = "client";

        var staff = WriteIdToken(issuer, audience, creds, "deb@townofwiley.gov", "Staff");
        var driver = WriteIdToken(issuer, audience, creds, "driver@townofwiley.gov", "Driver");

        var staffPrincipal = CognitoIdTokenValidator.Validate(staff, issuer, audience, [key]);
        var driverPrincipal = CognitoIdTokenValidator.Validate(driver, issuer, audience, [key]);

        staffPrincipal.Should().NotBeNull();
        CognitoAccess.IsAllowed(staffPrincipal).Should().BeTrue();
        staffPrincipal!.Identity!.Name.Should().Be("deb@townofwiley.gov");
        driverPrincipal.Should().NotBeNull();
        CognitoAccess.IsAllowed(driverPrincipal).Should().BeFalse();
    }

    private static string WriteIdToken(
        string issuer,
        string audience,
        SigningCredentials creds,
        string email,
        string group)
    {
        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims:
            [
                new Claim("token_use", "id"),
                new Claim("email", email),
                new Claim("cognito:groups", group),
                new Claim("sub", Guid.NewGuid().ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    [Fact]
    public void ShouldProbe_RunsOnceWhenDomainIsSetAndNoCookie()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Get;
        http.Request.Path = "/assistant";
        http.Request.Headers.Accept = "text/html";

        CognitoSilentAuth.ShouldProbe(http.Request, Config("auth.example.com")).Should().BeTrue();
    }

    [Fact]
    public void SafeReturnPath_DropsOffSiteAndTokenUrls()
    {
        CognitoSilentAuth.SafeReturnPath("https://evil.example/?access_token=abc").Should().Be("/");
        CognitoSilentAuth.SafeReturnPath("/assistant").Should().Be("/assistant");
    }

    private static IConfiguration Config(string? domain) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [CognitoSilentAuth.DomainConfigKey] = domain
            })
            .Build();
}
