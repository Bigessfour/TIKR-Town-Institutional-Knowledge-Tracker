using System.Security.Claims;

namespace TIKR.Web.Services;

/// <summary>
/// Town Hub rule: Staff, admin, and Clerk may open TIKR. Driver may not.
/// A token with no groups is allowed so an unstamped clerk is not locked out.
/// </summary>
public static class CognitoAccess
{
    public static bool IsAllowed(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return false;

        return IsAllowed(principal.FindAll("cognito:groups").Select(claim => claim.Value));
    }

    public static bool IsAllowed(IEnumerable<string>? groups)
    {
        var list = (groups ?? [])
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Select(group => group.Trim())
            .ToList();
        if (list.Count == 0)
            return true;

        return list.Any(group =>
            group.Equals("Staff", StringComparison.OrdinalIgnoreCase)
            || group.Equals("admin", StringComparison.OrdinalIgnoreCase)
            || group.Equals("Clerk", StringComparison.OrdinalIgnoreCase));
    }

    public static ClaimsPrincipal WithGroupRoles(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
            return principal;

        foreach (var group in identity.FindAll("cognito:groups").ToList())
        {
            if (!identity.HasClaim(ClaimTypes.Role, group.Value))
                identity.AddClaim(new Claim(ClaimTypes.Role, group.Value));
        }

        var email = identity.FindFirst("email")?.Value;
        if (!string.IsNullOrWhiteSpace(email) && identity.FindFirst(ClaimTypes.Email) is null)
            identity.AddClaim(new Claim(ClaimTypes.Email, email));

        return principal;
    }
}
