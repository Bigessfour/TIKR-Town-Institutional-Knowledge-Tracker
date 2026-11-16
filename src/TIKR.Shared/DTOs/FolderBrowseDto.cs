namespace TIKR.Shared.DTOs;

public record FolderBrowseEntryDto(string Name, string FullPath);

public record FolderBrowseResultDto(
    IReadOnlyList<string> Roots,
    string? CurrentPath,
    string? ParentPath,
    IReadOnlyList<FolderBrowseEntryDto> Entries,
    string? Error = null);
