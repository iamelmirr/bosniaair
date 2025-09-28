namespace SarajevoAir.Api.Enums;

/// <summary>
/// Types of air quality records stored in the database.
/// </summary>
public enum AirQualityRecordType
{
    /// <summary>
    /// Current live air quality measurements
    /// </summary>
    LiveSnapshot = 0,

    /// <summary>
    /// Forecasted air quality data for future days
    /// </summary>
    Forecast = 1
}
