using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class CouncilAgendaScaffoldTests
{
    [Fact]
    public void CreateOrderOfBusiness_FollowsDlgOrderOfBusiness()
    {
        var sections = CouncilAgendaScaffold.CreateOrderOfBusiness(
            "TOW",
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 7, 13));

        sections.Select(s => s.Title).Should().ContainInOrder(
            "Call to Order",
            "Approval of Minutes",
            "Public Comment",
            "Reports",
            "Old Business / Unfinished Business",
            "New Business",
            "Adjourn");
    }

    [Fact]
    public void PreviousSecondMonday_FromAugust_ReturnsJulySecondMonday()
    {
        CouncilMeetingSchedule.PreviousSecondMonday(new DateOnly(2026, 8, 10))
            .Should().Be(new DateOnly(2026, 7, 13));
    }
}

public class UnfinishedBusinessExtractorTests
{
    [Fact]
    public void Extract_FindsTabledAndContinuedLines()
    {
        const string text = """
            Budget amendment was tabled until the September meeting.
            Street project continued to next month for engineering review.
            Routine adjournment.
            """;

        var hits = UnfinishedBusinessExtractor.Extract(text);
        hits.Should().HaveCountGreaterThanOrEqualTo(2);
        hits[0].Title.Should().Contain("tabled", "extractor should surface tabled items");
    }
}
