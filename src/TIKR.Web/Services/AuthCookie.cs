namespace TIKR.Web.Services;

public static class AuthCookie
{
    public const string Name = "tikr-access-token";

    /// <summary>Browser-local GUID used for chat isolation when JWT auth is disabled.</summary>
    public const string ChatUserName = "tikr-chat-user";
}
