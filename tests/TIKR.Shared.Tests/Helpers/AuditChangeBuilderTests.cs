using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class AuditChangeBuilderTests
{
    [Fact]
    public void Build_WithNoChanges_ReturnsSummaryOnly()
    {
        AuditChangeBuilder.Build("Budget ordinance", ("Title", "A", "A"))
            .Should().Be("Budget ordinance");
    }

    [Fact]
    public void Build_WithFieldDiffs_ReturnsJsonPayload()
    {
        var json = AuditChangeBuilder.Build(
            "Budget ordinance",
            ("Title", "Old", "New"),
            ("IsCompleted", false, true));

        json.Should().Contain("\"summary\":\"Budget ordinance\"");
        json.Should().Contain("\"Title\"");
        json.Should().Contain("\"from\":\"Old\"");
        json.Should().Contain("\"to\":\"New\"");
        json.Should().Contain("\"IsCompleted\"");
    }
}

public class AuditDetailsFormatterTests
{
    [Fact]
    public void Format_PlainText_Passthrough()
    {
        AuditDetailsFormatter.Format("Created requirement")
            .Should().Be("Created requirement");
    }

    [Fact]
    public void Format_JsonDiffs_ReadableSummary()
    {
        var details = AuditChangeBuilder.Build("Budget", ("Title", "A", "B"));
        var formatted = AuditDetailsFormatter.Format(details);
        formatted.Should().Contain("Budget");
        formatted.Should().Contain("Title:");
        formatted.Should().Contain("A → B");
    }
}
