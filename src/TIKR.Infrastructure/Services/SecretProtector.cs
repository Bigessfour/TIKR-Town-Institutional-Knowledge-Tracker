using Microsoft.AspNetCore.DataProtection;

namespace TIKR.Infrastructure.Services;

/// <summary>Encrypts clerk-stored secrets at rest (AppSettings) via Data Protection.</summary>
public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedOrPlain);
    bool LooksProtected(string value);
}

public sealed class SecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    public const string Purpose = "TIKR.Secrets.v1";
    private const string Prefix = "dp1:";

    private readonly IDataProtector _protector = provider.CreateProtector(Purpose);

    public string Protect(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        return Prefix + _protector.Protect(plaintext.Trim());
    }

    public string Unprotect(string protectedOrPlain)
    {
        if (string.IsNullOrWhiteSpace(protectedOrPlain))
            return protectedOrPlain;

        if (!LooksProtected(protectedOrPlain))
            return protectedOrPlain;

        try
        {
            return _protector.Unprotect(protectedOrPlain[Prefix.Length..]);
        }
        catch
        {
            // Legacy or foreign ciphertext — return as-is for diagnostics; callers treat empty as missing.
            return protectedOrPlain;
        }
    }

    public bool LooksProtected(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith(Prefix, StringComparison.Ordinal);
}
