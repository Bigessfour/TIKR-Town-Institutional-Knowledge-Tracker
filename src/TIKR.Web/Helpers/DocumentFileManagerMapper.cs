using Syncfusion.Blazor.FileManager;
using TIKR.Shared.DTOs;
using TIKR.Shared.Helpers;

namespace TIKR.Web.Helpers;

/// <summary>
/// Maps Document library rows to Syncfusion <see cref="FileManagerDirectoryContent"/>
/// for the documented OnRead / event-driven File Manager pattern.
/// </summary>
public static class DocumentFileManagerMapper
{
    public static List<FileManagerDirectoryContent> ToFileManagerItems(
        IEnumerable<DocumentDto> documents,
        IEnumerable<string>? extraFolders = null)
    {
        return DocumentFileManagerLogic.BuildNodes(documents, extraFolders)
            .Select(ToContent)
            .ToList();
    }

    public static FileManagerDirectoryContent ToContent(DocumentFileManagerLogic.Node node) =>
        new()
        {
            Id = node.Id,
            ParentId = node.ParentId,
            Name = node.Name,
            IsFile = node.IsFile,
            HasChild = !node.IsFile,
            Size = node.Size,
            DateCreated = node.DateCreated,
            DateModified = node.DateModified,
            FilterPath = node.FilterPath,
            Type = node.Type,
            CaseSensitive = false,
            ShowHiddenItems = false
        };

    public static FileManagerResponse<FileManagerDirectoryContent> BuildReadResponse(
        IReadOnlyList<FileManagerDirectoryContent> all,
        string? path)
    {
        var response = new FileManagerResponse<FileManagerDirectoryContent>();
        var nodes = all.Select(FromContent).ToList();
        var cwdNode = DocumentFileManagerLogic.ResolveCwd(nodes, path);
        if (cwdNode is null)
        {
            response.Error = new ErrorDetails { Code = "404", Message = "Folder not found." };
            return response;
        }

        response.CWD = ToContent(cwdNode);
        response.Files = DocumentFileManagerLogic.GetChildren(nodes, cwdNode.Id)
            .Select(ToContent)
            .ToList();
        return response;
    }

    public static FileManagerResponse<FileManagerDirectoryContent> BuildSearchResponse(
        IReadOnlyList<FileManagerDirectoryContent> all,
        string searchText)
    {
        var nodes = all.Select(FromContent).ToList();
        var root = DocumentFileManagerLogic.GetRoot(nodes);
        var response = new FileManagerResponse<FileManagerDirectoryContent>
        {
            Files = DocumentFileManagerLogic.Search(nodes, searchText).Select(ToContent).ToList()
        };
        if (root is not null)
            response.CWD = ToContent(root);
        return response;
    }

    private static DocumentFileManagerLogic.Node FromContent(FileManagerDirectoryContent c) =>
        new(
            c.Id ?? "",
            c.ParentId,
            c.Name ?? "",
            c.IsFile,
            c.Size,
            c.DateCreated,
            c.DateModified,
            c.FilterPath ?? "",
            c.Type ?? "",
            DocumentFileManagerLogic.TryParseFileId(c.Id, out var id) ? id : null,
            null);
}
