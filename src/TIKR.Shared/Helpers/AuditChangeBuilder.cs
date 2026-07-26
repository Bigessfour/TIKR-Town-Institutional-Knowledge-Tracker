using System.Text.Json;

namespace TIKR.Shared.Helpers;

/// <summary>Builds JSON audit detail payloads with before/after field diffs.</summary>
public static class AuditChangeBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public static string Build(string summary, params (string Field, object? From, object? To)[] fields)
    {
        var changes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var (field, from, to) in fields)
        {
            var fromText = Normalize(from);
            var toText = Normalize(to);
            if (string.Equals(fromText, toText, StringComparison.Ordinal))
                continue;

            changes[field] = new Dictionary<string, string?>
            {
                ["from"] = fromText,
                ["to"] = toText
            };
        }

        if (changes.Count == 0)
            return summary;

        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["summary"] = summary,
            ["changes"] = changes
        }, JsonOptions);
    }

    private static string? Normalize(object? value) => value switch
    {
        null => null,
        string s => s,
        DateOnly d => d.ToString("yyyy-MM-dd"),
        DateTime dt => dt.ToUniversalTime().ToString("O"),
        Enum e => e.ToString(),
        bool b => b ? "true" : "false",
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
    };
}
