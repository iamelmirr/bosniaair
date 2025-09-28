/*
=== TIMEZONE UTILITY HELPER ===

PURPOSE: Centralized timezone management za Sarajevo (Central European Time)
Koristi se kroz ceo backend za konzistentno vrijeme

DESIGN:
- Static klasa za easy access
- Central European Standard Time timezone
- Automatic DST (Daylight Saving Time) handling
- Thread-safe operations

USAGE:
- TimeZoneHelper.GetSarajevoTime() - trenutno Sarajevo vrijeme
- TimeZoneHelper.ConvertToSarajevoTime(utcTime) - konvertuje UTC u Sarajevo vrijeme
*/

namespace SarajevoAir.Api.Utilities;

public static class TimeZoneHelper
{
    /// <summary>
    /// Sarajevo timezone - Central European Standard Time (CET/CEST)
    /// Automatski upravlja DST (Daylight Saving Time) switching
    /// </summary>
    private static readonly TimeZoneInfo SarajevoTimeZone = 
        TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

    /// <summary>
    /// Vraća trenutno vrijeme u Sarajevo timezone-u
    /// </summary>
    /// <returns>DateTime in Sarajevo local time</returns>
    public static DateTime GetSarajevoTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SarajevoTimeZone);
    }

    /// <summary>
    /// Konvertuje UTC vrijeme u Sarajevo lokalno vrijeme
    /// </summary>
    /// <param name="utcTime">UTC DateTime to convert</param>
    /// <returns>DateTime in Sarajevo local time</returns>
    public static DateTime ConvertToSarajevoTime(DateTime utcTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, SarajevoTimeZone);
    }

}
