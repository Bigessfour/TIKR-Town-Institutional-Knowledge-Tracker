using TIKR.Shared.DTOs;

namespace TIKR.Web.Helpers;

public static class LocalStatusFooterHelper
{
    public static string FormatLastSaved(DateTime? utc, DateTime utcNow)
    {
        if (utc is null)
            return "Last saved time unknown";

        var age = utcNow - utc.Value;
        if (age < TimeSpan.Zero)
            age = TimeSpan.Zero;

        if (age.TotalMinutes < 1)
            return "Last saved just now";
        if (age.TotalMinutes < 60)
            return $"Last saved {(int)age.TotalMinutes} min ago";
        if (age.TotalHours < 24)
            return $"Last saved {(int)age.TotalHours} hr ago";

        return $"Last saved {TownTime.Format(utc.Value)}";
    }

    public static string BuildFooterMessage(LocalStorageStatusDto status, DateTime utcNow) =>
        $"Town files stay on town storage • {status.TownName} • {FormatLastSaved(status.DataLastModifiedUtc, utcNow)}";

    public static string BuildConnectionHint(bool apiOffline, bool helperReady) =>
        apiOffline
            ? "The town server is offline. Files stay on town storage."
            : helperReady
                ? "Town files stay on town storage. The helper is ready."
                : "Town files stay on town storage. The helper is not ready.";
}
