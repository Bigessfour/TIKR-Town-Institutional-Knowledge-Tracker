using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace TIKR.Web.Services;

/// <summary>
/// Checks a Cognito ID token against the town pool JWKS. Keys are cached.
/// </summary>
public sealed class CognitoIdTokenValidator(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IList<SecurityKey>? _keys;
    private DateTimeOffset _keysUntil;

    public const string PoolIdConfigKey = "TIKR_COGNITO_USER_POOL_ID";
    public const string RegionConfigKey = "TIKR_COGNITO_REGION";
    public const string DefaultPoolId = "us-east-2_bHk9UcenK";
    public const string DefaultRegion = "us-east-2";

    public static string PoolId(IConfiguration configuration)
    {
        var id = configuration[PoolIdConfigKey];
        return string.IsNullOrWhiteSpace(id) ? DefaultPoolId : id.Trim();
    }

    public static string Region(IConfiguration configuration)
    {
        var region = configuration[RegionConfigKey];
        return string.IsNullOrWhiteSpace(region) ? DefaultRegion : region.Trim();
    }

    public static string Issuer(IConfiguration configuration) =>
        $"https://cognito-idp.{Region(configuration)}.amazonaws.com/{PoolId(configuration)}";

    public async Task<ClaimsPrincipal?> ValidateAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var keys = await GetKeysAsync(cancellationToken);
        if (keys is null || keys.Count == 0)
            return null;

        return Validate(token, Issuer(configuration), CognitoSilentAuth.ClientId(configuration), keys);
    }

    public static ClaimsPrincipal? Validate(
        string token,
        string issuer,
        string audience,
        IEnumerable<SecurityKey> keys)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKeys = keys,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                NameClaimType = "email",
                RoleClaimType = "cognito:groups"
            }, out var validated);

            if (validated is JwtSecurityToken jwt)
            {
                var use = jwt.Claims.FirstOrDefault(claim => claim.Type == "token_use")?.Value;
                if (!string.IsNullOrEmpty(use)
                    && !string.Equals(use, "id", StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            return CognitoAccess.WithGroupRoles(principal);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<IList<SecurityKey>?> GetKeysAsync(CancellationToken cancellationToken)
    {
        if (_keys is { Count: > 0 } && DateTimeOffset.UtcNow < _keysUntil)
            return _keys;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_keys is { Count: > 0 } && DateTimeOffset.UtcNow < _keysUntil)
                return _keys;

            var url = Issuer(configuration) + "/.well-known/jwks.json";
            var client = httpClientFactory.CreateClient("CognitoJwks");
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return _keys;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var set = new JsonWebKeySet(json);
            _keys = set.GetSigningKeys().ToList();
            _keysUntil = DateTimeOffset.UtcNow.AddHours(12);
            return _keys;
        }
        catch (Exception)
        {
            return _keys;
        }
        finally
        {
            _gate.Release();
        }
    }
}
