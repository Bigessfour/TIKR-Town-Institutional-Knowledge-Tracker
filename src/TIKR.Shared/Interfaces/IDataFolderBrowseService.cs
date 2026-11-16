using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

public interface IDataFolderBrowseService
{
    /// <summary>
    /// Lists allowlisted roots when <paramref name="path"/> is empty;
    /// otherwise lists immediate child directories under an allowlisted path.
    /// Optional <paramref name="query"/> filters by folder name (case-insensitive contains).
    /// </summary>
    FolderBrowseResultDto Browse(string? path = null, string? query = null);
}
