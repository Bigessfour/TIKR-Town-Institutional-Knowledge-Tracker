namespace TIKR.Web.Services;

public static class AuthCookie
{
    public const string Name = "tikr-access-token";

    /// <summary>Browser-local GUID, or <c>cognito:{sub}</c> when a ClerkSuite session is present.</summary>
    public const string ChatUserName = "tikr-chat-user";

    /// <summary>Display-only email. Never used as the chat key.</summary>
    public const string ChatDisplayName = "tikr-chat-display";

    /// <summary>Set after one silent Cognito attempt so the browser does not redirect again.</summary>
    public const string CognitoChecked = "tikr-cognito-checked";

    public const string PkceVerifier = "tikr-pkce";

    public const string AuthReturn = "tikr-auth-return";
}
