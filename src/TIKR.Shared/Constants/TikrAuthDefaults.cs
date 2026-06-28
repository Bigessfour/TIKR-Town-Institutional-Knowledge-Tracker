namespace TIKR.Shared.Constants;

public static class TikrAuthDefaults
{
    /// <summary>
    /// Placeholder when auth is disabled. Not used to protect production data — set TIKR_JWT_SIGNING_KEY when auth is on.
    /// </summary>
    public const string DevDisabledJwtSigningKey =
        "auth-disabled-placeholder-not-a-production-signing-key";
}
