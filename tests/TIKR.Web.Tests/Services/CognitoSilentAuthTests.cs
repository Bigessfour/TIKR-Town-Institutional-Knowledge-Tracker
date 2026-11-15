using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
    public void ShouldProbe_IsFalseAfterOneAttempt()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Get;
        http.Request.Path = "/assistant";
        http.Request.Headers.Accept = "text/html";
        http.Request.Headers.Cookie = $"{AuthCookie.CognitoChecked}=1";

        CognitoSilentAuth.ShouldProbe(http.Request, Config("auth.example.com")).Should().BeFalse();
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
