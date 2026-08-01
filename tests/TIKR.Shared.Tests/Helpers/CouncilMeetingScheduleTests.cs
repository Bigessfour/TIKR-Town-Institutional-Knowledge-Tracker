using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class CouncilMeetingScheduleTests
{
    [Theory]
    [InlineData(2026, 8, 2026, 8, 10)]
    [InlineData(2026, 9, 2026, 9, 14)]
    [InlineData(2026, 10, 2026, 10, 12)]
    [InlineData(2026, 11, 2026, 11, 9)]
    [InlineData(2026, 12, 2026, 12, 14)]
    public void SecondMonday_ReturnsExpectedDate(int year, int month, int expectedYear, int expectedMonth, int expectedDay)
    {
        CouncilMeetingSchedule.SecondMonday(year, month)
            .Should().Be(new DateOnly(expectedYear, expectedMonth, expectedDay));
    }

    [Fact]
    public void SecondMondaysInRange_AugThroughDec2026_ReturnsFiveDates()
    {
        var dates = CouncilMeetingSchedule.SecondMondaysInRange(2026, 8, 12).ToList();
        dates.Should().HaveCount(5);
        dates[0].Should().Be(new DateOnly(2026, 8, 10));
        dates[^1].Should().Be(new DateOnly(2026, 12, 14));
    }

    [Fact]
    public void DefaultLeadAndLag_AreTwoDays()
    {
        CouncilMeetingSchedule.DefaultAgendaLeadDays.Should().Be(2);
        CouncilMeetingSchedule.DefaultMinutesLagDays.Should().Be(2);
    }
}
