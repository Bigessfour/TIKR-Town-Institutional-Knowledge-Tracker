namespace TIKR.Web.Helpers;

/// <summary>
/// Clerk-facing times. The NAS container clock is UTC, so <see cref="DateTime.ToLocalTime"/>
/// shows the wrong hour. Wiley is America/Denver.
/// </summary>
public static class TownTime
{
    private static readonly TimeZoneInfo Denver = TimeZoneInfo.FindSystemTimeZoneById("America/Denver");

    public static DateTime ToDenver(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Utc => value,
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utc, Denver);
    }

    public static string Format(DateTime value) => ToDenver(value).ToString("g");
}
