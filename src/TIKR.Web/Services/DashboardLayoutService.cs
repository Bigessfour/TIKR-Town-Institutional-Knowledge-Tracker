namespace TIKR.Web.Services;

using System.Text.Json;
using TIKR.Shared.DTOs;

/// <summary>
/// Default clerk dashboard panel layout and localStorage persistence (key tikr-dashboard-layout-v1).
/// </summary>
public static class DashboardLayoutService
{
    public const string StorageKey = "tikr-dashboard-layout-v1";

    public static IReadOnlyList<DashboardLayoutPanelDto> DefaultPanels { get; } =
    [
        new("urgency-strip", 0, 0, 12, 1),
        new("quick-actions", 8, 1, 4, 2, 2, 1),
        new("due-out-grid", 0, 1, 8, 4, 4, 2),
        new("missing-packets", 0, 5, 6, 2, 3, 1),
        new("recent-activity", 6, 5, 6, 2, 3, 1),
        new("corpus-attention", 0, 7, 12, 1, 4, 1),
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
