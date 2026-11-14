using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class CurrentUserServiceTests
{
    [Fact]
    public void UserId_PrefersEmailThenNameIdentifier()
    {
        var email = new CurrentUserService(Accessor(new Claim(ClaimTypes.Email, "deb@townofwiley.gov"), new Claim(ClaimTypes.NameIdentifier, "deb-id")));
        email.UserId.Should().Be("deb@townofwiley.gov");
        email.IsAuthenticated.Should().BeTrue();

        var idOnly = new CurrentUserService(Accessor(new Claim(ClaimTypes.NameIdentifier, "deb-id")));
        idOnly.UserId.Should().Be("deb-id");
    }

    [Fact]
    public void UserId_IsNullWhenThereIsNoHttpContext()
    {
        var sut = new CurrentUserService(new HttpContextAccessor());

        sut.UserId.Should().BeNull();
        sut.IsAuthenticated.Should().BeFalse();
    }

    private static IHttpContextAccessor Accessor(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "test");
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
