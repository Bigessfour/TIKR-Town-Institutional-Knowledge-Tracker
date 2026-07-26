using TIKR.Shared.DTOs;

namespace TIKR.Web.Helpers;

public static class DocumentLibraryLogic
{
    public sealed class FolderNode
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? FolderKey { get; set; }
        public bool IsExpanded { get; set; } = true;
        public List<FolderNode> Children { get; set; } = [];
    }

    public static IEnumerable<DocumentDto> FilterVisible(
        IEnumerable<DocumentDto> documents,
        string? folderFilter,
        string searchMode,
        string searchQuery,
        HashSet<Guid>? semanticHitIds)
    {
        if (searchMode == "semantic" && semanticHitIds is { Count: > 0 })
        {
            var hitOrder = semanticHitIds.ToList();
            var hits = documents.Where(d => semanticHitIds.Contains(d.Id));
            if (folderFilter == "__UNCAT__")
                hits = hits.Where(d => string.IsNullOrWhiteSpace(d.SuggestedFolder));
            return hits.OrderBy(d => hitOrder.IndexOf(d.Id));
        }

        var folderFiltered = documents.Where(d =>
        {
            if (string.IsNullOrEmpty(folderFilter))
                return true;
            if (folderFilter == "__UNCAT__")
                return string.IsNullOrWhiteSpace(d.SuggestedFolder);
            return string.Equals(d.SuggestedFolder, folderFilter, StringComparison.OrdinalIgnoreCase);
        });

        return folderFiltered
            .Where(d =>
            {
                if (string.IsNullOrWhiteSpace(searchQuery))
                    return true;
                var q = searchQuery.Trim();
                var nameMatch = d.FileName.Contains(q, StringComparison.OrdinalIgnoreCase);
                var tagMatch = (d.AiTags ?? "").Contains(q, StringComparison.OrdinalIgnoreCase);
                var folderMatch = (d.SuggestedFolder ?? "").Contains(q, StringComparison.OrdinalIgnoreCase);
                return nameMatch || tagMatch || folderMatch;
            })
            .OrderByDescending(d => d.UploadedAt);
    }

    /// <summary>Maps UI folder filter to API folder (All and Uncategorized pass null).</summary>
    public static string? MapFolderFilterForApi(string? folderFilter) =>
        string.IsNullOrEmpty(folderFilter) || folderFilter == "__UNCAT__" ? null : folderFilter;

    public static List<FolderNode> BuildFolderTree(IReadOnlyList<DocumentDto> documents)
    {
        var groups = documents
            .GroupBy(d => d.SuggestedFolder?.Trim() ?? "")
            .OrderBy(g => g.Key);

        var nodes = new List<FolderNode>
        {
            new()
            {
                Id = "all",
                DisplayName = $"All Documents ({documents.Count})",
                FolderKey = null
            }
        };

        foreach (var g in groups)
        {
            var key = g.Key;
            var label = string.IsNullOrEmpty(key) ? "Uncategorized" : key;
            nodes.Add(new FolderNode
            {
                Id = "f_" + (string.IsNullOrEmpty(key) ? "uncat" : key.GetHashCode().ToString()),
                DisplayName = $"{label} ({g.Count()})",
                FolderKey = string.IsNullOrEmpty(key) ? "__UNCAT__" : key
            });
        }

        return nodes;
    }

    public static string DescribeSemanticResults(SemanticSearchResponse? result)
    {
        if (result is null)
            return "Semantic search unavailable (is Ollama running?).";

        if (!result.EmbeddingAvailable)
            return "Semantic search unavailable (is Ollama running with nomic-embed-text?).";

        if (result.Hits.Count == 0)
            return $"No semantic matches yet. {result.Considered} document(s) embedded so far.";

        return $"Showing {result.Hits.Count} semantic match(es) from {result.Considered} embedded document(s).";
    }
}
