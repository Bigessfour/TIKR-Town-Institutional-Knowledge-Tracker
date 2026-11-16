using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TIKR.Web.Services;

public class TikrAuthenticationStateProvider(
    IHttpContextAccessor httpContextAccessor,
    AuthSettings authSettings,
    CognitoIdTokenValidator cognitoTokens) : AuthenticationStateProvider, IHostEnvironmentAuthenticationStateProvider
{
    private Task<AuthenticationState>? _hostState;

    public void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask)
    {
        _hostState = authenticationStateTask ?? throw new ArgumentNullException(nameof(authenticationStateTask));
        NotifyAuthenticationStateChanged(_hostState);
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_hostState is not null)
            return await _hostState;

        var context = httpContextAccessor.HttpContext;
        if (context?.User.Identity?.IsAuthenticated == true)
            return new AuthenticationState(context.User);

        if (authSettings.CognitoGateEnabled
            && context?.Request.Cookies.TryGetValue(AuthCookie.CognitoSession, out var session) == true
            && !string.IsNullOrWhiteSpace(session))
        {
            var principal = await cognitoTokens.ValidateAsync(session);
            if (principal is not null && CognitoAccess.IsAllowed(principal))
                return new AuthenticationState(principal);
        }

        if (context?.Request.Cookies.TryGetValue(AuthCookie.Name, out var token) == true
            && !string.IsNullOrWhiteSpace(token))
        {
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                var identity = new ClaimsIdentity(
                    jwt.Claims,
                    authenticationType: "jwt",
                    nameType: ClaimTypes.Email,
                    roleType: ClaimTypes.Role);
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch (Exception)
            {
                /* local cookie was not a JWT */
            }
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public void NotifyAuthenticationChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
