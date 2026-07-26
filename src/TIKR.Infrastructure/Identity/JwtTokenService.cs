using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TIKR.Shared.Configuration;

namespace TIKR.Infrastructure.Identity;

public class JwtTokenService(IConfiguration configuration)
{
    public const string TokenTypeClaim = "tikr_token_type";
    public const string AccessTokenType = "access";
    public const string RefreshTokenType = "refresh";

    public (string Token, DateTime ExpiresAt) CreateToken(ApplicationUser user, IEnumerable<string> roles) =>
        CreateAccessToken(user, roles);

    public (string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt)
        CreateTokenPair(ApplicationUser user, IEnumerable<string> roles)
    {
        var (access, accessExpires) = CreateAccessToken(user, roles);
        var refreshDays = TikrConfiguration.GetJwtRefreshExpirationDays(configuration);
        var refreshExpires = DateTime.UtcNow.AddDays(refreshDays);
        var refresh = CreateJwt(user, roles, refreshExpires, RefreshTokenType);
        return (access, accessExpires, refresh, refreshExpires);
    }

    public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(refreshToken, CreateValidationParameters(), out var validated);
            if (validated is not JwtSecurityToken)
                return null;

            var typ = principal.FindFirst(TokenTypeClaim)?.Value;
            return typ == RefreshTokenType ? principal : null;
        }
        catch
        {
            return null;
        }
    }

    private (string Token, DateTime ExpiresAt) CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var expirationHours = TikrConfiguration.GetJwtExpirationHours(configuration);
        var expiresAt = DateTime.UtcNow.AddHours(expirationHours);
        return (CreateJwt(user, roles, expiresAt, AccessTokenType), expiresAt);
    }

    private string CreateJwt(ApplicationUser user, IEnumerable<string> roles, DateTime expiresAt, string tokenType)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? user.UserName ?? string.Empty),
            new(TokenTypeClaim, tokenType)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TikrConfiguration.GetJwtSigningKey(configuration)));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "tikr-api",
            audience: "tikr-web",
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TokenValidationParameters CreateValidationParameters() =>
        new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "tikr-api",
            ValidAudience = "tikr-web",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(TikrConfiguration.GetJwtSigningKey(configuration))),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
}
