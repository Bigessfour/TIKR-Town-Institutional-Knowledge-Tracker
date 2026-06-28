using FluentAssertions;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class AgentStorageCryptoTests
{
    private static readonly byte[] Key = Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");

    [Fact]
    public void EncryptDecrypt_RoundTripsPlaintext()
    {
        var plain = "Wiley mill levy filing notes"u8.ToArray();
        var envelope = AgentStorageCrypto.Encrypt(plain, Key);
        var decrypted = AgentStorageCrypto.Decrypt(envelope, Key);
        decrypted.Should().Equal(plain);
    }

    [Fact]
    public void TryParseKey_AcceptsValidBase64Key()
    {
        AgentStorageCrypto.TryParseKey(Convert.ToBase64String(Key), out var parsed).Should().BeTrue();
        parsed.Should().Equal(Key);
    }

    [Fact]
    public void TryParseKey_RejectsInvalidLength()
    {
        AgentStorageCrypto.TryParseKey(Convert.ToBase64String(new byte[16]), out _).Should().BeFalse();
    }
}
