namespace TIKR.Shared.TestFixtures;

/// <summary>
/// Non-production placeholders for auth-related unit and integration tests.
/// GitGuardian/gitleaks-safe — never use in docker/.env or production.
/// </summary>
public static class TestAuthFixtures
{
    public const string AdminEmail = "admin@test.gov";
    public const string ClerkEmail = "clerk@test.gov";
    public const string BootstrapPassword = "UnitTestBootstrapNotARealSecret1";
    public const string NewUserPassword = "UnitTestNewUserNotARealSecret1";
    public const string JwtSigningKey = "fake-hmac-only-for-unit-tests-not-real-credentials";
}
