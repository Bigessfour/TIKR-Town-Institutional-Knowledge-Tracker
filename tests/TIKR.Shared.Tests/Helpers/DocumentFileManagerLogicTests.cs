using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class DocumentFileManagerLogicTests
{
    [Fact]
    public void BuildNodes_CreatesRootFoldersAndFiles()
    {
        var docs = new[]
        {
            new DocumentDto(Guid.NewGuid(), "budget.pdf", "application/pdf", 100, null, "Finance", DateTime.UtcNow),
            new DocumentDto(Guid.NewGuid(), "scan.pdf", "application/pdf", 50, null, null, DateTime.UtcNow),
        };

        var nodes = DocumentFileManagerLogic.BuildNodes(docs);

        nodes.Should().Contain(n => n.Id == DocumentFileManagerLogic.RootId && !n.IsFile);
        nodes.Should().Contain(n => n.Name == "Finance" && !n.IsFile);
        nodes.Should().Contain(n => n.Name == DocumentFileManagerLogic.UncategorizedFolderName && !n.IsFile);
        nodes.Count(n => n.IsFile).Should().Be(2);
        nodes.Should().Contain(n => n.IsFile && n.Name == "budget.pdf" && n.DocumentId == docs[0].Id);
    }

    [Fact]
    public void ResolveCwd_RootAndFolderPaths()
    {
        var docs = new[]
        {
            new DocumentDto(Guid.NewGuid(), "a.pdf", null, 1, null, "Permits", DateTime.UtcNow)
        };
        var nodes = DocumentFileManagerLogic.BuildNodes(docs);

        DocumentFileManagerLogic.ResolveCwd(nodes, "/")!.Id.Should().Be(DocumentFileManagerLogic.RootId);
        DocumentFileManagerLogic.ResolveCwd(nodes, "/Permits/")!.Name.Should().Be("Permits");
    }

    [Fact]
    public void GetChildren_RootListsFoldersOnly()
    {
        var docs = new[]
        {
            new DocumentDto(Guid.NewGuid(), "a.pdf", null, 1, null, "Finance", DateTime.UtcNow)
        };
        var nodes = DocumentFileManagerLogic.BuildNodes(docs);
        var children = DocumentFileManagerLogic.GetChildren(nodes, DocumentFileManagerLogic.RootId);

        children.Should().OnlyContain(n => !n.IsFile);
        children.Should().Contain(n => n.Name == "Finance");
    }

    [Fact]
    public void TryParseFileId_RoundTrips()
    {
        var id = Guid.NewGuid();
        var nodeId = DocumentFileManagerLogic.FileNodeId(id);
        DocumentFileManagerLogic.TryParseFileId(nodeId, out var parsed).Should().BeTrue();
        parsed.Should().Be(id);
    }

    [Fact]
    public void Search_MatchesFileNames()
    {
        var docs = new[]
        {
            new DocumentDto(Guid.NewGuid(), "water-rate.pdf", null, 1, null, "Finance", DateTime.UtcNow),
            new DocumentDto(Guid.NewGuid(), "minutes.pdf", null, 1, null, "Minutes", DateTime.UtcNow),
        };
        var nodes = DocumentFileManagerLogic.BuildNodes(docs);
        var hits = DocumentFileManagerLogic.Search(nodes, "*water*");
        hits.Should().ContainSingle(n => n.Name == "water-rate.pdf");
    }

    [Fact]
    public void ExtraFolders_AppearEvenWithoutDocuments()
    {
        var nodes = DocumentFileManagerLogic.BuildNodes([], ["Archive"]);
        nodes.Should().Contain(n => n.Name == "Archive" && !n.IsFile);
    }

    [Fact]
    public void ToSuggestedFolder_NullsUncategorized()
    {
        DocumentFileManagerLogic.ToSuggestedFolder("Uncategorized").Should().BeNull();
        DocumentFileManagerLogic.ToSuggestedFolder("Finance").Should().Be("Finance");
    }
}
