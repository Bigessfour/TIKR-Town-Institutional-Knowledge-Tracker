using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class ChatClerkProfilesTests
{
    [Theory]
    [InlineData("deb", "deb")]
    [InlineData("Deb", "deb")]
    [InlineData("Deb Dillon", "deb")]
    [InlineData("local:paige", "paige")]
    [InlineData("Paige Lindo", "paige")]
    [InlineData("PAIGE", "paige")]
    public void TryNormalize_AcceptsNamedClerks(string raw, string expected)
    {
        ChatClerkProfiles.TryNormalize(raw, out var key).Should().BeTrue();
        key.Should().Be(expected);
    }

    [Fact]
    public void DisplayName_UsesFullNames()
    {
        ChatClerkProfiles.DisplayName("deb").Should().Be("Deb Dillon");
        ChatClerkProfiles.DisplayName("paige").Should().Be("Paige Lindo");
    }

    [Fact]
    public void ToUserId_PrefixesLocal()
    {
        ChatClerkProfiles.ToUserId("Deb Dillon").Should().Be("local:deb");
        ChatClerkProfiles.ToUserId("paige").Should().Be("local:paige");
    }

    [Theory]
    [InlineData("DESKTOP-KN6INHL", "deb")]
    [InlineData("desktop-kn6inhl", "deb")]
    [InlineData("DESKTOP-KN6INHL.town.local", "deb")]
    [InlineData("DESKTOP-O9TCKP1", "paige")]
    [InlineData("desktop-o9tckp1", "paige")]
    public void TryResolveFromMachineName_UsesNasBackupInventory(string machine, string expected)
    {
        ChatClerkProfiles.TryResolveFromMachineName(machine, out var key).Should().BeTrue();
        key.Should().Be(expected);
    }

    [Fact]
    public void TryResolveFromMachineName_UnknownHost_Fails()
    {
        ChatClerkProfiles.TryResolveFromMachineName("STEPHENS-MACBOOK-PRO", out _).Should().BeFalse();
    }

    [Fact]
    public void TryNormalize_RejectsUnknown()
    {
        ChatClerkProfiles.TryNormalize("anonymous", out _).Should().BeFalse();
        ChatClerkProfiles.TryNormalize(Guid.NewGuid().ToString("N"), out _).Should().BeFalse();
    }
}
