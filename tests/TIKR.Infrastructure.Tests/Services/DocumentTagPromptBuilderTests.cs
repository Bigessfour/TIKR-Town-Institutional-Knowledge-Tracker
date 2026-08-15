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
    public void Build_IncludesWileyFewShotExamples()
    {
        var prompt = DocumentTagPromptBuilder.Build("doc.pdf", "preview text");

        prompt.Should().Contain("Town of Wiley");
        prompt.Should().Contain("Prowers County");
        prompt.Should().Contain("Board of Trustees");
        prompt.Should().Contain("304 Main Street");
        prompt.Should().Contain("COUNCIL MEETINGS/2024-08-12 minutes.pdf");
        prompt.Should().Contain("\"suggestedFolder\": \"Minutes\"");
        prompt.Should().Contain("COUNCIL MEETINGS/2025-03-10 agenda.pdf");
        prompt.Should().Contain("\"suggestedFolder\": \"Agenda\"");
        prompt.Should().Contain("BUDGET/2026 mill levy certification.pdf");
        prompt.Should().Contain("Budget / Finance");
        prompt.Should().Contain("ORDINANCES/Ordinance_2024-05.pdf");
        prompt.Should().Contain("Ordinances");
        prompt.Should().Contain("{\"tags\":");
        prompt.Should().Contain("JSON only");
        prompt.Should().NotContain("Stephen_Resume.pdf");
        prompt.Should().NotContain("Town of Example");
    }

    [Fact]
    public void Build_IncludesFileNameAndPreview()
    {
        var prompt = DocumentTagPromptBuilder.Build("my-file.pdf", "unique preview body xyz");

        prompt.Should().Contain("File name: my-file.pdf");
        prompt.Should().Contain("Content preview: unique preview body xyz");
    }

    [Fact]
    public void Build_PrefersLibraryRelativePathOverLeafName()
    {
        var prompt = DocumentTagPromptBuilder.Build(
            "agenda.pdf",
            "preview",
            "COUNCIL MEETINGS/2024/agenda.pdf");

        prompt.Should().Contain("File name: COUNCIL MEETINGS/2024/agenda.pdf");
        prompt.Should().NotContain("File name: agenda.pdf");
    }

    [Fact]
    public void TaggingTemperature_IsLowForDeterminism()
    {
        DocumentTagPromptBuilder.TaggingTemperature.Should().BeInRange(0.1f, 0.2f);
    }
}
