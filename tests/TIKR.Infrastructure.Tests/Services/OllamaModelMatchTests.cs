using FluentAssertions;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class OllamaModelMatchTests
{
    [Theory]
    [InlineData("nomic-embed-text", true)]
    [InlineData("nomic-embed-text:latest", true)]
    [InlineData("llama3.2:3b", false)]
    public void HasModel_MatchesExactOrPrefixedNames(string installed, bool expected)
    {
        OllamaChatClientFactory.HasModel([installed], "nomic-embed-text")
            .Should().Be(expected);
    }

    [Fact]
    public void BuildStatusMessage_ReportsMissingEmbedModel()
    {
        var message = OllamaChatClientFactory.BuildStatusMessage(
            "llama3.2:3b", "nomic-embed-text", chatPresent: true, embedPresent: false, loadedCount: 1);

        message.Should().Contain("nomic-embed-text");
        message.Should().Contain("ollama pull");
    }
}
