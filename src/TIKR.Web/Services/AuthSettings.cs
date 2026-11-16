namespace TIKR.Web.Services;

public sealed class AuthSettings
{
    public bool IsEnabled { get; init; }

    /// <summary>Town Hosted UI is configured. Document requests without a session go to Cognito.</summary>
    public bool CognitoGateEnabled { get; init; }
}
