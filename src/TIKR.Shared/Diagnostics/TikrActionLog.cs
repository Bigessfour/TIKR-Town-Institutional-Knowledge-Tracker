using Microsoft.Extensions.Logging;

namespace TIKR.Shared.Diagnostics;

/// <summary>
/// Consistent structured action logs for clerk-invoked flows (buttons, API mutations, AI).
/// Grep logs with: <c>Action </c> or filter property <c>Action</c>.
/// </summary>
public static class TikrActionLog
{
    public static void Started(ILogger logger, string action, string? detail = null)
    {
        if (detail is null)
            logger.LogInformation("Action {Action} {Phase}", action, "started");
        else
            logger.LogInformation("Action {Action} {Phase} {Detail}", action, "started", detail);
    }

    public static void Completed(ILogger logger, string action, string? detail = null, long? durationMs = null)
    {
        if (durationMs is null && detail is null)
            logger.LogInformation("Action {Action} {Phase}", action, "completed");
        else if (durationMs is null)
            logger.LogInformation("Action {Action} {Phase} {Detail}", action, "completed", detail);
        else if (detail is null)
            logger.LogInformation("Action {Action} {Phase} DurationMs={DurationMs}", action, "completed", durationMs);
        else
            logger.LogInformation(
                "Action {Action} {Phase} {Detail} DurationMs={DurationMs}",
                action, "completed", detail, durationMs);
    }

    public static void Failed(ILogger logger, string action, Exception ex, string? detail = null)
    {
        if (detail is null)
            logger.LogError(ex, "Action {Action} {Phase} Error={Error}", action, "failed", ex.Message);
        else
            logger.LogError(ex, "Action {Action} {Phase} {Detail} Error={Error}", action, "failed", detail, ex.Message);
    }

    public static void Failed(ILogger logger, string action, string error, string? detail = null)
    {
        if (detail is null)
            logger.LogWarning("Action {Action} {Phase} Error={Error}", action, "failed", error);
        else
            logger.LogWarning("Action {Action} {Phase} {Detail} Error={Error}", action, "failed", detail, error);
    }

    public static void Info(ILogger logger, string action, string detail) =>
        logger.LogInformation("Action {Action} {Phase} {Detail}", action, "info", detail);
}
