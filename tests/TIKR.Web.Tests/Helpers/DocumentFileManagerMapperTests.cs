using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Shared.Helpers;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Helpers;

public class DocumentFileManagerMapperTests
{
    [Fact]
    public void BuildReadResponse_RootListsFolders()
    {
        var docs = new List<DocumentDto>
        {
            new(Guid.NewGuid(), "fee.pdf", "application/pdf", 10, null, "Finance", DateTime.UtcNow)
        };
        var all = DocumentFileManagerMapper.ToFileManagerItems(docs);
        var response = DocumentFileManagerMapper.BuildReadResponse(all, "/");

        response.Error.Should().BeNull();
        response.CWD.Should().NotBeNull();
        response.CWD!.Id.Should().Be(DocumentFileManagerLogic.RootId);
        response.Files.Should().NotBeNull();
        response.Files!.Should().Contain(f => f.Name == "Finance" && !f.IsFile);
    }

    [Fact]
    public void BuildReadResponse_FolderListsFiles()
    {
        var id = Guid.NewGuid();
        var docs = new List<DocumentDto>
        {
            new(id, "fee.pdf", "application/pdf", 10, null, "Finance", DateTime.UtcNow)
        };
        var all = DocumentFileManagerMapper.ToFileManagerItems(docs);
        var response = DocumentFileManagerMapper.BuildReadResponse(all, "/Finance/");

        response.Files!.Should().ContainSingle(f => f.IsFile && f.Name == "fee.pdf");
        DocumentFileManagerLogic.TryParseFileId(response.Files![0].Id, out var parsed).Should().BeTrue();
        parsed.Should().Be(id);
    }

    [Fact]
    public void BuildSearchResponse_FindsByName()
    {
        var docs = new List<DocumentDto>
        {
            new(Guid.NewGuid(), "aqueduct-levy.txt", "text/plain", 5, null, "Finance", DateTime.UtcNow),
            new(Guid.NewGuid(), "other.txt", "text/plain", 5, null, "Finance", DateTime.UtcNow),
        };
        var all = DocumentFileManagerMapper.ToFileManagerItems(docs);
        var response = DocumentFileManagerMapper.BuildSearchResponse(all, "aqueduct");
        response.Files!.Should().ContainSingle(f => f.Name.Contains("aqueduct"));
    }
}
