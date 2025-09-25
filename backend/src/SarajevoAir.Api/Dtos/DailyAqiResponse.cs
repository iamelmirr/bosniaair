namespace SarajevoAir.Api.Dtos;

/*
===========================================================================================
                              DAILY AQI HISTORICAL DATA TRANSFER OBJECTS
===========================================================================================

PURPOSE & TREND ANALYSIS:
DTOs za historical air quality data presentation i trend visualization.
Enables weekly pattern analysis i long-term air quality monitoring.

HISTORICAL DATA CHARACTERISTICS:
- 7-day rolling window za trend identification
- Database-sourced data (not external API calls)
- Daily aggregated averages za consistent comparison
- Day names za user-friendly temporal navigation

ANALYTICS & VISUALIZATION SUPPORT:
Data structured za chart libraries i dashboard components
Day names enable intuitive weekly pattern recognition  
Color coding provides immediate visual assessment
Consistent date formatting za timeline presentations
*/

/*
=== SINGLE DAY HISTORICAL ENTRY ===

DAILY AGGREGATION STRUCTURE:
Represents single day's aggregated air quality data
Combines temporal metadata sa AQI metrics za complete day view
*/

/// <summary>
/// Historical air quality data za single day
/// Contains aggregated daily AQI sa presentation metadata
/// </summary>
/// <param name="Date">Date u ISO format (YYYY-MM-DD)</param>
/// <param name="DayName">Full day name (Monday, Tuesday, etc.) za user display</param>
/// <param name="ShortDay">Abbreviated day name (Mon, Tue, etc.) za compact UI</param>
/// <param name="Aqi">Daily average AQI value</param>
/// <param name="Category">EPA AQI category za daily average</param>
/// <param name="Color">Hex color code za visual indicators i charts</param>
public record DailyAqiEntry(
    string Date,
    string DayName,
    string ShortDay,
    int Aqi,
    string Category,
    string Color
);

/*
=== HISTORICAL PERIOD RESPONSE ===

MULTI-DAY TREND CONTAINER:
Aggregates multiple daily entries za trend analysis
Includes metadata about analysis period i data freshness
*/

/// <summary>
/// Complete historical AQI response za specified time period
/// Contains chronologically ordered daily aggregations
/// 
/// JSON Response Example:
/// {
///   "city": "Sarajevo",
///   "period": "Last 7 days",
///   "data": [
///     {
///       "date": "2024-03-09",
///       "dayName": "Saturday",
///       "shortDay": "Sat", 
///       "aqi": 87,
///       "category": "Moderate",
///       "color": "#FFFF00"
///     },
///     {
///       "date": "2024-03-10",
///       "dayName": "Sunday",
///       "shortDay": "Sun",
///       "aqi": 92,
///       "category": "Moderate", 
///       "color": "#FFFF00"
///     }
///   ],
///   "timestamp": "2024-03-15T14:30:00Z"
/// }
/// </summary>
/// <param name="City">Target city za historical analysis</param>
/// <param name="Period">Human-readable description od analysis timeframe</param>
/// <param name="Data">Chronologically ordered daily AQI entries</param>
/// <param name="Timestamp">UTC timestamp kada je analysis performed</param>
public record DailyAqiResponse(
    string City,
    string Period,
    IReadOnlyList<DailyAqiEntry> Data,
    DateTime Timestamp
);
