using TIKR.Shared.DTOs;

namespace TIKR.Web.Services;

public interface IAuthSessionService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    string? GetAccessToken();
}

public class AuthSessionService(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    TikrAuthenticationStateProvider authStateProvider) : IAuthSessionService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("TikrAuth");
        var response = await client.PostAsJsonAsync("/api/auth/login", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
        if (login is null)
            return null;

        var context = httpContextAccessor.HttpContext;
        if (context is not null)
        {
            context.Response.Cookies.Append(AuthCookie.Name, login.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = login.ExpiresAt
            });
        }

        authStateProvider.NotifyAuthenticationChanged();
        return login;
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is not null)
            context.Response.Cookies.Delete(AuthCookie.Name);

        authStateProvider.NotifyAuthenticationChanged();
        return Task.CompletedTask;
    }

    public string? GetAccessToken()
    {
        var context = httpContextAccessor.HttpContext;
        return context?.Request.Cookies.TryGetValue(AuthCookie.Name, out var token) == true
            ? token
            : null;
    }
}
