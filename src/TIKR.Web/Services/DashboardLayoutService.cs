namespace TIKR.Web.Services;

using System.Text.Json;
using TIKR.Shared.DTOs;

/// <summary>
/// Default clerk dashboard panel layout and localStorage persistence.
/// v2 drops a saved v1 layout so the taller due-out rows apply. Reset layout still restores these defaults.
/// </summary>
public static class DashboardLayoutService
{
    public const string StorageKey = "tikr-dashboard-layout-v2";

    public static IReadOnlyList<DashboardLayoutPanelDto> DefaultPanels { get; } =
    [
        new("urgency-strip", 0, 0, 12, 1),
        new("quick-actions", 8, 1, 4, 3, 2, 2),
        new("due-out-grid", 0, 1, 8, 4, 4, 2),
        new("missing-packets", 0, 5, 6, 3, 3, 2),
        new("recent-activity", 6, 5, 6, 3, 3, 2),
        new("corpus-attention", 0, 8, 12, 2, 4, 1),
    ];

    public static string Serialize(IEnumerable<DashboardLayoutPanelDto> panels) =>
        JsonSerializer.Serialize(panels);

    public static IReadOnlyList<DashboardLayoutPanelDto>? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var panels = JsonSerializer.Deserialize<List<DashboardLayoutPanelDto>>(json);
            return panels is { Count: > 0 } ? panels : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
