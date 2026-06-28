using System.Text.Json;
using System.Text.Json.Serialization;

namespace TIKR.Web.Services;

public record ColoradoResource(
    string Name,
    string Url,
    string Kind,
    IReadOnlyList<string> Topics,
    string Summary);

/// <summary>
/// Loads the curated catalog of trusted external sources a Colorado town clerk
/// can be referred to. Data lives in <c>Data/colorado-clerk-resources.json</c>
/// so non-developers can edit entries without touching code.
/// </summary>
public class ColoradoResourceCatalog
{
    private readonly IReadOnlyList<ColoradoResource> _resources;
    private readonly string? _lastReviewed;

    public ColoradoResourceCatalog(IReadOnlyList<ColoradoResource> resources, string? lastReviewed)
    {
        _resources = resources;
        _lastReviewed = lastReviewed;
    }

    public IReadOnlyList<ColoradoResource> Resources => _resources;

    public string? LastReviewed => _lastReviewed;

    /// <summary>
    /// Renders the catalog as a compact block suitable for injection into
    /// an LLM system prompt. Falls back to an empty string if the catalog
    /// is empty so the prompt remains valid.
    /// </summary>
    public string ToSystemPromptBlock()
    {
        if (_resources.Count == 0) return string.Empty;

        var lines = _resources.Select(r =>
        {
            var url = string.IsNullOrWhiteSpace(r.Url) ? "(internal contact)" : r.Url;
            return $"- {r.Name} — {url} — {r.Summary}";
        });

        return string.Join("\n", lines);
    }

    public static ColoradoResourceCatalog LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return new ColoradoResourceCatalog(Array.Empty<ColoradoResource>(), null);

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        var doc = JsonSerializer.Deserialize<CatalogFile>(json, options);
        return new ColoradoResourceCatalog(
            (IReadOnlyList<ColoradoResource>?)doc?.Resources ?? Array.Empty<ColoradoResource>(),
            doc?.LastReviewed);
    }

    private sealed class CatalogFile
    {
        [JsonPropertyName("lastReviewed")]
        public string? LastReviewed { get; set; }

        [JsonPropertyName("resources")]
        public List<ColoradoResource> Resources { get; set; } = new();
    }
}
