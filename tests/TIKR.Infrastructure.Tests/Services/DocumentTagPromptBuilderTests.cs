using FluentAssertions;
using TIKR.Infrastructure.Services;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentTagPromptBuilderTests
{
    [Fact]
    public void Build_IncludesFolderVocabulary()
    {
        var prompt = DocumentTagPromptBuilder.Build("doc.pdf", "preview text");

        foreach (var folder in DocumentTagHeuristics.FolderVocabulary)
            prompt.Should().Contain(folder);
    }

    [Fact]
    public void Build_IncludesFewShotExamples()
    {
        var prompt = DocumentTagPromptBuilder.Build("doc.pdf", "preview text");

        prompt.Should().Contain("Stephen_Resume.pdf");
        prompt.Should().Contain("Personnel / HR");
        prompt.Should().Contain("budget-2026.pdf");
        prompt.Should().Contain("Budget / Finance");
        prompt.Should().Contain("council-minutes-2026-03-12.pdf");
        prompt.Should().Contain("\"suggestedFolder\": \"Minutes\"");
        prompt.Should().Contain("Ordinance_12.pdf");
        prompt.Should().Contain("Ordinances");
        prompt.Should().Contain("{\"tags\":");
        prompt.Should().Contain("JSON only");
    }

    [Fact]
    public void Build_IncludesFileNameAndPreview()
    {
        var prompt = DocumentTagPromptBuilder.Build("my-file.pdf", "unique preview body xyz");

        prompt.Should().Contain("File name: my-file.pdf");
        prompt.Should().Contain("Content preview: unique preview body xyz");
    }

    [Fact]
    public void TaggingTemperature_IsLowForDeterminism()
    {
        DocumentTagPromptBuilder.TaggingTemperature.Should().BeInRange(0.1f, 0.2f);
    }
}
