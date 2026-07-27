using TIKR.Shared.DTOs;

namespace TIKR.Shared.Helpers;

/// <summary>
/// Builds a virtual File Manager tree from Document library metadata
/// (AI SuggestedFolder as folders; Document rows as files). Used by Browse mode
/// so Syncfusion SfFileManager can follow documented OnRead event patterns
/// without a separate physical-provider service.
/// </summary>
public static class DocumentFileManagerLogic
{
    public const string RootId = "root";
    public const string UncategorizedFolderName = "Uncategorized";
    public const string UncategorizedFolderId = "folder:__uncat__";

    public sealed record Node(
        string Id,
        string? ParentId,
        string Name,
        bool IsFile,
        long Size,
        DateTime DateCreated,
        DateTime DateModified,
        string FilterPath,
        string Type,
        Guid? DocumentId,
        string? SuggestedFolder);

    public static string FolderId(string folderName) =>
        string.IsNullOrWhiteSpace(folderName) || IsUncategorized(folderName)
            ? UncategorizedFolderId
            : "folder:" + folderName.Trim();

    public static string FileNodeId(Guid documentId) => "file:" + documentId.ToString("N");

    public static bool TryParseFileId(string? id, out Guid documentId)
    {
        documentId = default;
        if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("file:", StringComparison.Ordinal))
            return false;
        return Guid.TryParseExact(id["file:".Length..], "N", out documentId)
               || Guid.TryParse(id["file:".Length..], out documentId);
    }

    public static bool TryParseFolderId(string? id, out string folderName)
    {
        folderName = string.Empty;
        if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("folder:", StringComparison.Ordinal))
            return false;
        var key = id["folder:".Length..];
        if (key is "__uncat__")
        {
            folderName = UncategorizedFolderName;
            return true;
        }

        folderName = key;
        return !string.IsNullOrWhiteSpace(folderName);
    }

    public static bool IsUncategorized(string? folder) =>
        string.IsNullOrWhiteSpace(folder)
        || string.Equals(folder.Trim(), UncategorizedFolderName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Flat list of root + folder nodes + file nodes suitable for Syncfusion OnRead navigation.
    /// </summary>
    public static List<Node> BuildNodes(
        IEnumerable<DocumentDto> documents,
        IEnumerable<string>? extraFolders = null)
    {
        var docs = documents?.ToList() ?? [];
        var folderNames = docs
            .Select(d => string.IsNullOrWhiteSpace(d.SuggestedFolder) ? UncategorizedFolderName : d.SuggestedFolder.Trim())
            .Concat(extraFolders ?? [])
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var nodes = new List<Node>
        {
            new(
                RootId,
                ParentId: null,
                Name: "Documents",
                IsFile: false,
                Size: docs.Sum(d => d.FileSizeBytes),
                DateCreated: DateTime.UtcNow,
                DateModified: DateTime.UtcNow,
                FilterPath: "",
                Type: "folder",
                DocumentId: null,
                SuggestedFolder: null)
        };

        foreach (var folder in folderNames)
        {
            var id = FolderId(folder);
            var filesInFolder = docs.Where(d => FolderMatches(d.SuggestedFolder, folder)).ToList();
            nodes.Add(new(
                id,
                ParentId: RootId,
                Name: folder,
                IsFile: false,
                Size: filesInFolder.Sum(d => d.FileSizeBytes),
                DateCreated: filesInFolder.Count > 0 ? filesInFolder.Min(d => d.UploadedAt) : DateTime.UtcNow,
                DateModified: filesInFolder.Count > 0 ? filesInFolder.Max(d => d.UploadedAt) : DateTime.UtcNow,
                FilterPath: $"/{folder}/",
                Type: "folder",
                DocumentId: null,
                SuggestedFolder: IsUncategorized(folder) ? null : folder));
        }

        foreach (var doc in docs)
        {
            var folder = string.IsNullOrWhiteSpace(doc.SuggestedFolder)
                ? UncategorizedFolderName
                : doc.SuggestedFolder.Trim();
            var parentId = FolderId(folder);
            var ext = Path.GetExtension(doc.FileName);
            nodes.Add(new(
                FileNodeId(doc.Id),
                ParentId: parentId,
                Name: doc.FileName,
                IsFile: true,
                Size: doc.FileSizeBytes,
                DateCreated: doc.UploadedAt,
                DateModified: doc.UploadedAt,
                FilterPath: $"/{folder}/",
                Type: string.IsNullOrEmpty(ext) ? "" : ext,
                DocumentId: doc.Id,
                SuggestedFolder: IsUncategorized(folder) ? null : folder));
        }

        return nodes;
    }

    public static Node? GetRoot(IReadOnlyList<Node> nodes) =>
        nodes.FirstOrDefault(n => n.Id == RootId);

    public static IReadOnlyList<Node> GetChildren(IReadOnlyList<Node> nodes, string parentId) =>
        nodes.Where(n => string.Equals(n.ParentId, parentId, StringComparison.Ordinal)).ToList();

    /// <summary>
    /// Resolve OnRead path ("/", "/Finance/") to the CWD node.
    /// </summary>
    public static Node? ResolveCwd(IReadOnlyList<Node> nodes, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path is "/" or "\\")
            return GetRoot(nodes);

        var trimmed = path.Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrEmpty(trimmed))
            return GetRoot(nodes);

        var segment = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        return nodes.FirstOrDefault(n =>
            !n.IsFile && string.Equals(n.Name, segment, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<Node> Search(IReadOnlyList<Node> nodes, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return [];
        var q = searchText.Trim().Trim('*');
        return nodes
            .Where(n => n.IsFile && n.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Folder name to persist on Document.SuggestedFolder (null for Uncategorized).
    /// </summary>
    public static string? ToSuggestedFolder(string? folderName) =>
        IsUncategorized(folderName) ? null : folderName?.Trim();

    private static bool FolderMatches(string? suggestedFolder, string folderName)
    {
        if (IsUncategorized(folderName))
            return string.IsNullOrWhiteSpace(suggestedFolder);
        return string.Equals(suggestedFolder?.Trim(), folderName.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
