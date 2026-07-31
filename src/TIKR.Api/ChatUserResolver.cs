using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using TIKR.Shared.Configuration;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Api;

/// <summary>Resolves the clerk key for chat isolation — never a shared "anonymous" bucket.</summary>
public static class ChatUserResolver
{
    public static string? TryResolve(ICurrentUserService currentUser, HttpRequest request, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(currentUser.UserId))
            return currentUser.UserId.Trim();

        // Auth on: fail closed — do not fall back to a shared key.
        if (TikrConfiguration.IsAuthEnabled(configuration))
            return null;

        // Auth off (local/dev): isolate by browser-issued GUID header.
        if (request.Headers.TryGetValue(ChatHistoryLimits.ChatUserHeaderName, out var values))
        {
            var raw = values.FirstOrDefault()?.Trim();
            if (Guid.TryParse(raw, out var id))
                return $"local:{id:N}";
        }

        return null;
    }
}
