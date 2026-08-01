using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class ActionedAgendaLineExtractorTests
{
    [Fact]
    public void Extract_PrefersSubstantiveAgendaLines()
    {
        const string text = """
            TOWN OF WILEY — REGULAR MEETING AGENDA
            August 10, 2026

            1. Call to Order
            2. Public Comment
            Budget amendment ordinance (first reading)
            Water rate resolution
            Adjourn
            """;

        var lines = ActionedAgendaLineExtractor.Extract(text);

        lines.Should().Contain("Call to Order");
        lines.Should().Contain("Budget amendment ordinance (first reading)");
        lines.Should().NotContain(l => l.Contains("TOWN OF WILEY", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Extract_ReturnsEmptyForBlankText()
    {
        ActionedAgendaLineExtractor.Extract(null).Should().BeEmpty();
        ActionedAgendaLineExtractor.Extract("   ").Should().BeEmpty();
    }
}
