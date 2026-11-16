using FluentAssertions;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Helpers;

public class TownTimeTests
{
    [Fact]
    public void ToDenver_UsesMountainStandardTimeInWinter()
    {
        var utc = new DateTime(2026, 1, 15, 19, 0, 0, DateTimeKind.Utc);
        TownTime.ToDenver(utc).Should().Be(new DateTime(2026, 1, 15, 12, 0, 0));
    }

    [Fact]
    public void ToDenver_UsesMountainDaylightTimeInSummer()
    {
        var utc = new DateTime(2026, 7, 15, 18, 0, 0, DateTimeKind.Utc);
        TownTime.ToDenver(utc).Should().Be(new DateTime(2026, 7, 15, 12, 0, 0));
    }

    [Fact]
    public void ToDenver_ConvertsALocalClockThroughUtc()
    {
        var local = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Local);
        TownTime.ToDenver(local).Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Fact]
    public void ToDenver_TreatsUnspecifiedClockAsUtc()
    {
        var stored = new DateTime(2026, 1, 15, 19, 0, 0, DateTimeKind.Unspecified);
        TownTime.ToDenver(stored).Hour.Should().Be(12);
    }
}
