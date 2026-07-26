using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class DueOutFieldParserTests
{
    [Fact]
    public void Parse_ExtractsEmailPhoneDueAndSubmitTo()
    {
        var text = """
            Liquor License Renewal
            Submit to: Colorado Department of Revenue
            Contact: Jane Smith
            Due date: 03/15/2027
            Email jane.smith@example.com
            Phone (303) 555-0199
            """;

        var parsed = DueOutFieldParser.Parse(text);
        parsed.SubmitTo.Should().Contain("Colorado Department of Revenue");
        parsed.ContactName.Should().Be("Jane Smith");
        parsed.ContactEmail.Should().Be("jane.smith@example.com");
        parsed.ContactPhone.Should().Contain("303");
        parsed.DueDate.Should().Be(new DateOnly(2027, 3, 15));
    }

    [Fact]
    public void Parse_EmptyText_ReturnsEmpty()
    {
        var parsed = DueOutFieldParser.Parse("   ");
        parsed.DueDate.Should().BeNull();
        parsed.ContactEmail.Should().BeNull();
    }
}
