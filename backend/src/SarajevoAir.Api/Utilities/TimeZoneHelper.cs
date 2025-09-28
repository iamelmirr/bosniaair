namespace SarajevoAir.Api.Utilities;

/// <summary>
/// Utility class for handling Sarajevo timezone conversions.
/// Provides methods to work with Central European Time (CET/CEST).
/// </summary>
public static class TimeZoneHelper
{
    /// <summary>
    /// TimeZoneInfo for Central European Standard Time (Sarajevo timezone)
    /// </summary>
    private static readonly TimeZoneInfo SarajevoTimeZone = 
        TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

    /// <summary>
    /// Gets the current time in Sarajevo timezone.
    /// </summary>
    /// <returns>Current DateTime in Sarajevo timezone</returns>
    public static DateTime GetSarajevoTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SarajevoTimeZone);
    }

    /// <summary>
    /// Converts a UTC DateTime to Sarajevo timezone.
    /// </summary>
    /// <param name="utcTime">The UTC time to convert</param>
    /// <returns>DateTime in Sarajevo timezone</returns>
    public static DateTime ConvertToSarajevoTime(DateTime utcTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, SarajevoTimeZone);
    }

}
