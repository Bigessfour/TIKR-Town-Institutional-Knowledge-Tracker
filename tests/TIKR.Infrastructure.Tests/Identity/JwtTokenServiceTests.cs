using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TIKR.Infrastructure.Identity;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Identity;

[Trait("Category", TestCategories.FullyTested)]
public class JwtTokenServiceTests
{
    [Fact]
    public void CreateTokenPair_ReturnsAccessAndRefreshTokens()
    {
        var config = BuildConfig();
        var sut = new JwtTokenService(config);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = "deb@wiley.gov",
            UserName = "deb@wiley.gov"
        };

        var (access, accessExpires, refresh, refreshExpires) =
            sut.CreateTokenPair(user, ["Clerk"]);

        access.Should().NotBeNullOrWhiteSpace();
        refresh.Should().NotBeNullOrWhiteSpace();
        accessExpires.Should().BeAfter(DateTime.UtcNow);
        refreshExpires.Should().BeAfter(accessExpires);
    }

    [Fact]
    public void ValidateRefreshToken_AcceptsRefreshToken_RejectsAccessToken()
    {
        var config = BuildConfig();
        var sut = new JwtTokenService(config);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = "paige@wiley.gov",
            UserName = "paige@wiley.gov"
        };

        var (access, _, refresh, _) = sut.CreateTokenPair(user, ["Admin"]);

        sut.ValidateRefreshToken(refresh).Should().NotBeNull();
        sut.ValidateRefreshToken(access).Should().BeNull();
        sut.ValidateRefreshToken("not-a-jwt").Should().BeNull();
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_SIGNING_KEY"] = new string('k', 32),
                ["JWT_EXPIRATION_HOURS"] = "8",
                ["JWT_REFRESH_EXPIRATION_DAYS"] = "14"
            })
            .Build();
}
