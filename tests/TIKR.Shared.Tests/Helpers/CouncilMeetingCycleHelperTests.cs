using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class CouncilMeetingCycleHelperTests
{
    [Fact]
    public void TryParse_ExtractsKindBoardAndMeetingDate()
    {
        var description =
            "Council meeting cycle 2026 (Aug-Dec); kind=DraftMinutes; board=TOW; meeting=2026-08-10. Draft minutes.";

        CouncilMeetingCycleHelper.TryParse(description, out var kind, out var board, out var meetingDate)
            .Should().BeTrue();

        kind.Should().Be("DraftMinutes");
        board.Should().Be("TOW");
        meetingDate.Should().Be(new DateOnly(2026, 8, 10));
    }

    [Fact]
    public void Matches_ReturnsTrueForExactCycleRow()
    {
        var description =
            "Council meeting cycle 2026 (Aug-Dec); kind=PostAgenda; board=WSD; meeting=2026-09-14.";

        CouncilMeetingCycleHelper.Matches(description, "WSD", new DateOnly(2026, 9, 14), "PostAgenda")
            .Should().BeTrue();
    }

    [Fact]
    public void TryParse_HandlesTrailingSentenceAfterMeetingDate()
    {
        var description =
            "Council meeting cycle 2026 (Aug-Dec); kind=PostAgenda; board=TOW; meeting=2026-08-10. Post/build agenda at least 2 days before.";

        CouncilMeetingCycleHelper.Matches(description, "TOW", new DateOnly(2026, 8, 10), "PostAgenda")
            .Should().BeTrue();
    }

    [Fact]
    public void Matches_ReturnsFalseWhenBoardMissing()
    {
        var description =
            "Council meeting cycle 2026 (Aug-Dec); kind=PostAgenda; meeting=2026-08-10.";

        CouncilMeetingCycleHelper.Matches(description, "TOW", new DateOnly(2026, 8, 10), "PostAgenda")
            .Should().BeFalse();
    }
}

public class CouncilMinutesFileNamingTests
{
    [Fact]
    public void SuggestFileName_MatchesNasStyle()
    {
        CouncilMinutesFileNaming.SuggestFileName(new DateOnly(2026, 8, 10), "TOW")
            .Should().Be("8 AUGUST 10 2026 TOW.DOCX");
    }
}
