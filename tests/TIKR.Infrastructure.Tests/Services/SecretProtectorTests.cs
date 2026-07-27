using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using TIKR.Infrastructure.Services;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class SecretProtectorTests
{
    [Fact]
    public void Protect_RoundTrips_And_UsesPrefix()
    {
        var protector = CreateProtector();
        var sealedValue = protector.Protect("xai-secret-key");
        sealedValue.Should().StartWith("dp1:");
        protector.Unprotect(sealedValue).Should().Be("xai-secret-key");
    }

    [Fact]
    public void Unprotect_LeavesLegacyPlaintextAlone()
    {
        var protector = CreateProtector();
        protector.Unprotect("plain-legacy-key").Should().Be("plain-legacy-key");
    }

    private static SecretProtector CreateProtector()
    {
        var provider = DataProtectionProvider.Create(nameof(SecretProtectorTests));
        return new SecretProtector(provider);
    }
}
