using Microsoft.Extensions.Configuration;
using TIKR.Shared.DTOs;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public sealed class DataFolderBrowseService(IConfiguration configuration) : IDataFolderBrowseService
{
    public FolderBrowseResultDto Browse(string? path = null, string? query = null)
    {
        var roots = TikrDataPathRoots.Resolve(configuration);
        var filter = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

        if (string.IsNullOrWhiteSpace(path))
        {
            var rootEntries = roots
                .Where(Directory.Exists)
                .Select(r => new FolderBrowseEntryDto(Path.GetFileName(r.TrimEnd(Path.DirectorySeparatorChar)) is { Length: > 0 } name
                    ? name
                    : r, r))
                .Where(e => filter is null || e.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                            || e.FullPath.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new FolderBrowseResultDto(roots, null, null, rootEntries);
        }

        string full;
        try
        {
            full = Path.GetFullPath(path.Trim());
        }
        catch
        {
            return new FolderBrowseResultDto(roots, null, null, [], "That folder path is not valid.");
        }

        if (!TikrDataPathRoots.IsUnderAllowedRoot(full, roots))
        {
            return new FolderBrowseResultDto(
                roots,
                null,
                null,
                [],
                "Folders must be under town storage, a mounted network/external drive, or the TIKR data folder.");
        }

        if (!Directory.Exists(full))
        {
            return new FolderBrowseResultDto(roots, full, ParentIfAllowed(full, roots), [], "That folder does not exist.");
        }

        IReadOnlyList<FolderBrowseEntryDto> entries;
        try
        {
            entries = Directory.EnumerateDirectories(full)
                .Select(d =>
                {
                    var name = Path.GetFileName(d);
                    return new FolderBrowseEntryDto(string.IsNullOrEmpty(name) ? d : name, Path.GetFullPath(d));
                })
                .Where(e => filter is null
                            || e.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (UnauthorizedAccessException)
        {
            return new FolderBrowseResultDto(roots, full, ParentIfAllowed(full, roots), [], "Cannot read that folder.");
        }
        catch (IOException ex)
        {
            return new FolderBrowseResultDto(roots, full, ParentIfAllowed(full, roots), [], ex.Message);
        }

        return new FolderBrowseResultDto(roots, full, ParentIfAllowed(full, roots), entries);
    }

    private static string? ParentIfAllowed(string full, IReadOnlyList<string> roots)
    {
        var parent = Directory.GetParent(full)?.FullName;
        if (string.IsNullOrWhiteSpace(parent))
            return null;
        return TikrDataPathRoots.IsUnderAllowedRoot(parent, roots) ? parent : null;
    }
}
