using FluentAssertions;
using TIKR.Infrastructure.Services;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentIntakePromptBuilderTests
{
    [Fact]
    public void Build_IncludesFileNameExcerptAndJsonShape()
    {
        var prompt = DocumentIntakePromptBuilder.Build("notice.pdf", "Due April 1, 2026 to County Clerk");

        prompt.Should().Contain("File name: notice.pdf");
        prompt.Should().Contain("Due April 1, 2026 to County Clerk");
        prompt.Should().Contain("dueDates");
        prompt.Should().Contain("contacts");
        prompt.Should().Contain("actionItems");
        prompt.Should().Contain("JSON only");
        prompt.Should().Contain("Town of Wiley");
    }

    [Fact]
    public void Build_TruncatesLongExcerpt()
    {
        var longText = new string('x', DocumentIntakePromptBuilder.MaxExcerptChars + 500);
        var prompt = DocumentIntakePromptBuilder.Build("big.pdf", longText);

        prompt.Should().Contain(new string('x', DocumentIntakePromptBuilder.MaxExcerptChars));
        prompt.Should().NotContain(longText);
    }
}
