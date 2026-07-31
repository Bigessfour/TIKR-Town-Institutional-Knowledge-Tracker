using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class UserMemoryFactExtractorTests
{
    [Theory]
    [InlineData("My birthday is March 15", "birthday", "March 15")]
    [InlineData("Please call me Deb", "preferred_name", "Deb")]
    [InlineData("Remember that the mill levy packet is in the blue binder", "note", "the mill levy packet is in the blue binder")]
    public void Extract_RecognizesCommonClerkPhrases(string text, string key, string value)
    {
        var facts = UserMemoryFactExtractor.Extract(text);
        facts.Should().ContainSingle(f => f.Key == key && f.Value == value);
    }

    [Fact]
    public void Extract_IgnoresUnrelatedText()
    {
        UserMemoryFactExtractor.Extract("What is due this week?").Should().BeEmpty();
    }

    [Fact]
    public void FormatForPrompt_IncludesKnownFactsBlock()
    {
        var block = UserMemoryFactExtractor.FormatForPrompt([("birthday", "March 15")]);
        block.Should().Contain("Known facts about this clerk");
        block.Should().Contain("birthday: March 15");
    }
}
