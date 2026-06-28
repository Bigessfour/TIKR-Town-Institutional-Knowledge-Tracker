namespace TIKR.Api.Tests.Fixtures;

public static class TestAuthConstants
{
    // Not a real secret — gitleaks-safe placeholder for JWT HMAC in tests.
    public const string JwtSigningKey = "fake-hmac-only-for-unit-tests-not-real-credentials";
}
