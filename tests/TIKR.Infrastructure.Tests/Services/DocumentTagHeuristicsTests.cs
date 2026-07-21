using FluentAssertions;
using TIKR.Infrastructure.Services;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentTagHeuristicsTests
{
    [Theory]
    [InlineData("Stephen_Resume.pdf", null, DocumentTagHeuristics.PersonnelHr)]
    [InlineData("jane-cv.docx", null, DocumentTagHeuristics.PersonnelHr)]
    [InlineData("budget-2026.pdf", null, DocumentTagHeuristics.BudgetFinance)]
    [InlineData("Ordinance_12.pdf", null, DocumentTagHeuristics.Ordinances)]
    [InlineData("council-minutes.pdf", null, DocumentTagHeuristics.Minutes)]
    public void FillGaps_InfersFolderFromFilename(string fileName, string? content, string expectedFolder)
    {
        var (tags, folder) = DocumentTagHeuristics.FillGaps(fileName, content, [], null);
        folder.Should().Be(expectedFolder);
        tags.Should().NotBeEmpty();
    }

    [Fact]
    public void FillGaps_DoesNotOverrideAiFolderOrTags()
    {
        var (tags, folder) = DocumentTagHeuristics.FillGaps(
            "Stephen_Resume.pdf",
            null,
            ["custom"],
            "Correspondence");

        tags.Should().BeEquivalentTo(["custom"]);
        folder.Should().Be("Correspondence");
    }

    [Fact]
    public void FillGaps_UsesContentWhenFilenameGeneric()
    {
        var (tags, folder) = DocumentTagHeuristics.FillGaps(
            "document.pdf",
            "Curriculum Vitae\nExperience: Town Clerk",
            [],
            null);

        folder.Should().Be(DocumentTagHeuristics.PersonnelHr);
        tags.Should().Contain("resume");
    }
}
