namespace SarajevoAir.Api.Utilities;

public static class TimeZoneHelper
{
    private static readonly TimeZoneInfo SarajevoTimeZone = 
        TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

    public static DateTime GetSarajevoTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SarajevoTimeZone);
    }

    public static DateTime ConvertToSarajevoTime(DateTime utcTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, SarajevoTimeZone);
    }

}
