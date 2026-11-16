namespace TIKR.Web.Services;

public static class AuthCookie
{
    public const string Name = "tikr-access-token";

    /// <summary>Browser-local GUID, or <c>cognito:{sub}</c> when a ClerkSuite session is present.</summary>
    public const string ChatUserName = "tikr-chat-user";

    /// <summary>Display-only email. Never used as the chat key.</summary>
    public const string ChatDisplayName = "tikr-chat-display";

    /// <summary>Validated Cognito ID token. Presence means the clerk finished Hosted UI.</summary>
    public const string CognitoSession = "tikr-cognito-session";

    /// <summary>Set on a prompt=none start so login_required can retry with the town sign-in page.</summary>
    public const string CognitoSilent = "tikr-cognito-silent";

    /// <summary>Legacy skip cookie. No longer written. A session is required instead.</summary>
    public const string CognitoChecked = "tikr-cognito-checked";

    public const string PkceVerifier = "tikr-pkce";

    public const string AuthReturn = "tikr-auth-return";
}
