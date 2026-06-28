using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Helpers;

public class DocumentLibraryLogicTests
{
    private static DocumentDto Doc(Guid id, string name, string? folder = null, string? tags = null) =>
        new(id, name, "text/plain", 100, tags, folder, DateTime.UtcNow);

    [Fact]
    public void BuildFolderTree_IncludesAllAndGroupedFolders()
    {
        var docs = new List<DocumentDto>
        {
            Doc(Guid.NewGuid(), "a.pdf", "Finance"),
            Doc(Guid.NewGuid(), "b.pdf"),
            Doc(Guid.NewGuid(), "c.pdf", "Finance")
        };

        var tree = DocumentLibraryLogic.BuildFolderTree(docs);
        tree.Should().HaveCount(3);
        tree[0].DisplayName.Should().Be("All Documents (3)");
        tree.Should().Contain(n => n.DisplayName.StartsWith("Finance (2)"));
        tree.Should().Contain(n => n.DisplayName.StartsWith("Uncategorized (1)"));
    }

    [Fact]
    public void FilterVisible_FullTextSearch_MatchesFilenameTagsAndFolder()
    {
        var id = Guid.NewGuid();
        var docs = new[] { Doc(id, "budget.pdf", "Finance", "[\"levy\"]") };

        DocumentLibraryLogic.FilterVisible(docs, null, "full", "budget", null)
            .Should().ContainSingle().Which.Id.Should().Be(id);
        DocumentLibraryLogic.FilterVisible(docs, null, "full", "levy", null)
            .Should().ContainSingle();
        DocumentLibraryLogic.FilterVisible(docs, null, "full", "Finance", null)
            .Should().ContainSingle();
        DocumentLibraryLogic.FilterVisible(docs, null, "full", "missing", null)
            .Should().BeEmpty();
    }

    [Fact]
    public void FilterVisible_SemanticMode_UsesHitOrder()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var docs = new[]
        {
            Doc(first, "first.pdf"),
            Doc(second, "second.pdf")
        };
        var hits = new HashSet<Guid> { second, first };

        var visible = DocumentLibraryLogic.FilterVisible(docs, null, "semantic", "q", hits).ToList();
        visible.Select(d => d.Id).Should().ContainInOrder(second, first);
    }

    [Fact]
    public void FilterVisible_FolderFilter_UsesUncategorizedBucket()
    {
        var uncat = Guid.NewGuid();
        var docs = new[] { Doc(uncat, "misc.txt"), Doc(Guid.NewGuid(), "a.pdf", "Finance") };

        DocumentLibraryLogic.FilterVisible(docs, "__UNCAT__", "full", "", null)
            .Should().ContainSingle().Which.Id.Should().Be(uncat);
    }

    [Theory]
    [InlineData(null, "Semantic search unavailable (is Ollama running?).")]
    [InlineData(0, "No semantic matches yet. 2 document(s) embedded so far.")]
    [InlineData(1, "Showing 1 semantic match(es) from 3 embedded document(s).")]
    public void DescribeSemanticResults_FormatsStatus(int? hitCount, string expected)
    {
        SemanticSearchResponse? response = hitCount switch
        {
            null => null,
            0 => new SemanticSearchResponse("q", 2, []),
            _ => new SemanticSearchResponse("q", 3, [
                new SemanticSearchHit(Guid.NewGuid(), "a.pdf", null, "snippet", 0.9)
            ])
        };

        DocumentLibraryLogic.DescribeSemanticResults(response).Should().Be(expected);
    }
}
