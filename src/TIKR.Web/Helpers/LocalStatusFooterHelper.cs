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

        return $"Last saved {utc.Value.ToLocalTime():g}";
    }

    public static string BuildFooterMessage(LocalStorageStatusDto status, DateTime utcNow) =>
        $"All data stays in {status.TownName} • {status.StorageLabel} • {FormatLastSaved(status.DataLastModifiedUtc, utcNow)}";

    public static string BuildConnectionHint(bool apiOffline, bool ollamaAvailable) =>
        apiOffline
            ? "API offline — your data is still on the NAS"
            : ollamaAvailable
                ? "Local-first on Synology • Ollama ready"
                : "Local-first on Synology • Ollama offline";
}
