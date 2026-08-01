namespace TIKR.Shared.Helpers;

/// <summary>Wiley regular meeting schedule helpers (2nd Monday of each month).</summary>
public static class CouncilMeetingSchedule
{
    public const int DefaultAgendaLeadDays = 2;
    public const int DefaultMinutesLagDays = 2;

    /// <summary>Returns the 2nd Monday of the given month.</summary>
    public static DateOnly SecondMonday(int year, int month)
    {
        var date = new DateOnly(year, month, 1);
        while (date.DayOfWeek != DayOfWeek.Monday)
            date = date.AddDays(1);

        return date.AddDays(7);
    }

    public static IEnumerable<DateOnly> SecondMondaysInRange(int year, int firstMonth, int lastMonth)
    {
        if (lastMonth < firstMonth)
            yield break;

        for (var month = firstMonth; month <= lastMonth; month++)
            yield return SecondMonday(year, month);
    }

    /// <summary>Prior regular meeting (previous month's 2nd Monday).</summary>
    public static DateOnly PreviousSecondMonday(DateOnly meetingDate)
    {
        if (meetingDate.Month == 1)
            return SecondMonday(meetingDate.Year - 1, 12);

        return SecondMonday(meetingDate.Year, meetingDate.Month - 1);
    }
}
