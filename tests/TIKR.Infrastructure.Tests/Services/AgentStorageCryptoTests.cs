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
    public void Encrypt_RejectsWrongKeyLength()
    {
        var act = () => AgentStorageCrypto.Encrypt("x"u8.ToArray(), new byte[16]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrypt_RejectsShortEnvelope()
    {
        var act = () => AgentStorageCrypto.Decrypt(new byte[8], Key);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryParseKey_RejectsInvalidBase64()
    {
        AgentStorageCrypto.TryParseKey("not!!!valid", out _).Should().BeFalse();
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
