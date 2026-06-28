using System.Security.Cryptography;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// AES-256-GCM envelope for agent-scan blobs on the NAS volume.
/// Wire format: [12-byte nonce][16-byte tag][ciphertext].
/// </summary>
public static class AgentStorageCrypto
{
    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const int KeySize = 32;

    public static byte[] Encrypt(ReadOnlySpan<byte> plainText, ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Agent storage key must be {KeySize} bytes.", nameof(key));

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plainText.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainText, cipher, tag);

        var envelope = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(envelope.AsSpan(0, NonceSize));
        tag.CopyTo(envelope.AsSpan(NonceSize, TagSize));
        cipher.CopyTo(envelope.AsSpan(NonceSize + TagSize));
        return envelope;
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> envelope, ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Agent storage key must be {KeySize} bytes.", nameof(key));

        if (envelope.Length < NonceSize + TagSize)
            throw new ArgumentException("Envelope too short.", nameof(envelope));

        var nonce = envelope[..NonceSize];
        var tag = envelope.Slice(NonceSize, TagSize);
        var cipher = envelope[(NonceSize + TagSize)..];
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }

    public static bool TryParseKey(string? base64Key, out byte[] key)
    {
        key = [];
        if (string.IsNullOrWhiteSpace(base64Key))
            return false;

        try
        {
            key = Convert.FromBase64String(base64Key.Trim());
            return key.Length == KeySize;
        }
        catch
        {
            return false;
        }
    }
}
