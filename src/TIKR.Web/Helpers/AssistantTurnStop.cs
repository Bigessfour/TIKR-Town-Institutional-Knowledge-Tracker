namespace TIKR.Web.Helpers;

/// <summary>
/// The assistant Stop button and an Ollama timeout both surface as
/// <see cref="OperationCanceledException"/>. Only the Stop button cancels the user token.
/// </summary>
public static class AssistantTurnStop
{
    public static bool IsUserStop(CancellationToken userToken) => userToken.IsCancellationRequested;
}
