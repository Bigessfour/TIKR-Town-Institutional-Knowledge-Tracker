using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using TIKR.Shared.Helpers;

namespace TIKR.Web.Services;

/// <summary>
/// One silent ClerkSuite Hosted UI check. No password form. Tokens stay off the URL.
/// </summary>
public static class CognitoSilentAuth
{
    public const string ClientIdConfigKey = "TIKR_COGNITO_CLIENT_ID";
    public const string DomainConfigKey = "TIKR_COGNITO_DOMAIN";
    public const string ClientSecretConfigKey = "TIKR_COGNITO_CLIENT_SECRET";
    public const string DefaultClientId = "258binbcvsms0rfqj5g20qakki";

    public static string? HostedUiDomain(IConfiguration configuration)
    {
        var domain = configuration[DomainConfigKey];
        return string.IsNullOrWhiteSpace(domain) ? null : domain.Trim().TrimEnd('/');
    }

    public static string ClientId(IConfiguration configuration)
    {
        var id = configuration[ClientIdConfigKey];
        return string.IsNullOrWhiteSpace(id) ? DefaultClientId : id.Trim();
    }

    public static bool ShouldProbe(HttpRequest request, IConfiguration configuration)
    {
        if (HostedUiDomain(configuration) is null)
            return false;

        if (!HttpMethods.IsGet(request.Method))
            return false;

        if (!IsDocumentNavigation(request))
            return false;

        if (request.Cookies.ContainsKey(AuthCookie.CognitoChecked))
            return false;

        if (request.Cookies.TryGetValue(AuthCookie.ChatUserName, out var chatUser)
            && CognitoChatIdentity.TryParse(chatUser, out _))
            return false;

        return true;
    }

    public static bool IsDocumentNavigation(HttpRequest request)
    {
        var path = request.Path.Value ?? "/";
        if (path.StartsWith("/auth/", StringComparison.OrdinalIgnoreCase))
            return false;
        if (path.StartsWith("/_", StringComparison.OrdinalIgnoreCase))
            return false;
        if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            return false;
        if (path.Contains('.', StringComparison.Ordinal))
            return false;

        var accept = request.Headers.Accept.ToString();
        return string.IsNullOrEmpty(accept)
               || accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
               || accept.Contains("*/*", StringComparison.OrdinalIgnoreCase);
    }

    public static string SafeReturnPath(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return "/";

        var value = candidate.Trim();
        if (!value.StartsWith('/') || value.StartsWith("//", StringComparison.Ordinal))
            return "/";
        if (value.Contains("://", StringComparison.Ordinal) || value.Contains('\\'))
            return "/";
        if (value.StartsWith("/auth/", StringComparison.OrdinalIgnoreCase))
            return "/";

        return value;
    }

    public static string BuildAuthorizeUrl(
        string hostedUiDomain,
        string clientId,
        string redirectUri,
        string state,
        string codeChallenge)
    {
        var domain = hostedUiDomain.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? hostedUiDomain
            : "https://" + hostedUiDomain;
        var query = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["scope"] = "openid email",
            ["prompt"] = "none",
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state
        };
        return QueryHelpers.AddQueryString(domain.TrimEnd('/') + "/oauth2/authorize", query);
    }

    public static string CreateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64Url(bytes);
    }

    public static string CreateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64Url(hash);
    }

    public static string CreateState() => Base64Url(RandomNumberGenerator.GetBytes(16));

    public static bool TryReadIdentity(string idToken, string clientId, out Guid sub, out string? email)
    {
        sub = default;
        email = null;
        if (string.IsNullOrWhiteSpace(idToken))
            return false;

        JwtSecurityToken jwt;
        try
        {
            jwt = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
        }
        catch
        {
            return false;
        }

        if (!jwt.Audiences.Any(aud => string.Equals(aud, clientId, StringComparison.Ordinal)))
            return false;

        var subValue = jwt.Subject ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (!Guid.TryParse(subValue, out sub))
            return false;

        email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        if (!string.IsNullOrWhiteSpace(email) && email.Contains('@'))
            email = email.Trim();
        else
            email = null;

        return true;
    }

    public static async Task<string?> ExchangeCodeAsync(
        HttpClient http,
        string hostedUiDomain,
        string clientId,
        string? clientSecret,
        string redirectUri,
        string code,
        string codeVerifier,
        CancellationToken ct)
    {
        var domain = hostedUiDomain.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? hostedUiDomain
            : "https://" + hostedUiDomain;
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier
        };
        if (!string.IsNullOrWhiteSpace(clientSecret))
            form["client_secret"] = clientSecret;

        using var response = await http.PostAsync(
            domain.TrimEnd('/') + "/oauth2/token",
            new FormUrlEncodedContent(form),
            ct);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.TryGetProperty("id_token", out var token) ? token.GetString() : null;
    }

    public static CookieOptions SessionCookie(HttpRequest request) => new()
    {
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.Lax,
        Secure = request.IsHttps,
        MaxAge = TimeSpan.FromDays(30),
        Path = "/"
    };

    public static string RedirectUri(HttpRequest request) =>
        $"{request.Scheme}://{request.Host}/auth/cognito-callback";

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
