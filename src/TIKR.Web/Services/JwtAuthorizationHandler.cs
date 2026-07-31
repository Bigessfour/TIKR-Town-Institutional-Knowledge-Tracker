using TIKR.Shared.Helpers;

namespace TIKR.Web.Services;

/// <summary>
/// Ensures a durable browser chat-user cookie exists (auth-off isolation) and
/// forwards JWT + <see cref="ChatHistoryLimits.ChatUserHeaderName"/> to the API.
/// </summary>
public class JwtAuthorizationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is not null)
        {
            if (context.Request.Cookies.TryGetValue(AuthCookie.Name, out var token)
                && !string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var chatUser = EnsureChatUserCookie(context);
            if (!string.IsNullOrWhiteSpace(chatUser)
                && !request.Headers.Contains(ChatHistoryLimits.ChatUserHeaderName))
            {
                request.Headers.TryAddWithoutValidation(ChatHistoryLimits.ChatUserHeaderName, chatUser);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static string EnsureChatUserCookie(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(AuthCookie.ChatUserName, out var existing)
            && Guid.TryParse(existing, out _))
        {
            return existing;
        }

        var created = Guid.NewGuid().ToString("N");
        if (!context.Response.HasStarted)
        {
            context.Response.Cookies.Append(AuthCookie.ChatUserName, created, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                MaxAge = TimeSpan.FromDays(400)
            });
        }

        return created;
    }
}
