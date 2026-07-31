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

        // Auth off (Deb/Paige shared PC or local dev): isolate by header.
        if (!request.Headers.TryGetValue(ChatHistoryLimits.ChatUserHeaderName, out var values))
            return null;

        var raw = values.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // Named clerk profiles (preferred for shared trusted PC).
        if (ChatClerkProfiles.TryNormalize(raw, out var profileKey))
            return ChatClerkProfiles.ToUserId(profileKey);

        // Browser-issued GUID (tests / legacy cookies).
        if (Guid.TryParse(raw, out var id))
            return $"local:{id:N}";

        // Already-prefixed local ids from prior requests.
        if (raw.StartsWith("local:", StringComparison.OrdinalIgnoreCase)
            && raw.Length > "local:".Length)
        {
            var rest = raw["local:".Length..];
            if (ChatClerkProfiles.TryNormalize(rest, out var nested))
                return ChatClerkProfiles.ToUserId(nested);
            if (Guid.TryParse(rest, out var nestedGuid))
                return $"local:{nestedGuid:N}";
        }

        return null;
    }
}
