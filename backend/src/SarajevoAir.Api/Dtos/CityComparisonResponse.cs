namespace SarajevoAir.Api.Dtos;

/*
===========================================================================================
                            CITY COMPARISON DATA TRANSFER OBJECTS  
===========================================================================================

PURPOSE & COMPARATIVE ANALYSIS:
DTOs za multi-city air quality comparison i ranking operations.
Enables side-by-side analysis za travel planning i regional assessment.

COMPARISON CHALLENGES:
- Parallel API calls may succeed/fail independently
- Different cities may have different data availability
- Temporal synchronization across multiple data sources
- Graceful degradation za partial failures

DESIGN FOR RESILIENCE:
Nullable properties handle missing data scenarios
Error field captures specific failure reasons za individual cities
Comparison metadata tracks temporal consistency
*/

/*
=== INDIVIDUAL CITY COMPARISON ENTRY ===

RESILIENT CITY DATA STRUCTURE:
Handles success i failure scenarios za individual city data
Nullable properties enable partial success scenarios
*/

/// <summary>
/// Air quality data za single city u comparative analysis
/// Supports partial success scenarios sa nullable properties
/// </summary>
/// <param name="City">City name being compared</param>
/// <param name="Aqi">Current AQI value (null if data unavailable)</param>
/// <param name="Category">EPA AQI category based on current value</param>
/// <param name="Color">Hex color code za visual comparison charts</param>
/// <param name="DominantPollutant">Primary pollutant driving AQI (null if unavailable)</param>
/// <param name="Timestamp">UTC timestamp od data collection (null on failure)</param>
/// <param name="Error">Error message if city data collection failed</param>
public record CityComparisonEntry(
    string City,
    int? Aqi,
    string Category,
    string Color,
    string? DominantPollutant,
    DateTime? Timestamp,
    string? Error = null
);

/*
=== MULTI-CITY COMPARISON RESPONSE ===

AGGREGATED COMPARISON RESULTS:
Contains all city comparisons sa metadata za analysis quality
Enables ranking, filtering, i trend identification across cities
*/

/// <summary>
/// Complete multi-city air quality comparison response
/// Contains all requested cities sa success/failure indicators
/// 
/// JSON Response Example:
/// {
///   "cities": [
///     {
///       "city": "Sarajevo",
///       "aqi": 87,
///       "category": "Moderate",
///       "color": "#FFFF00", 
///       "dominantPollutant": "PM2.5",
///       "timestamp": "2024-03-15T14:30:00Z",
///       "error": null
///     },
///     {
///       "city": "InvalidCity",
///       "aqi": null,
///       "category": "Unknown",
///       "color": "#CCCCCC",
///       "dominantPollutant": null,
///       "timestamp": null,
///       "error": "City not found in monitoring network"
///     }
///   ],
///   "comparedAt": "2024-03-15T14:30:00Z",
///   "totalCities": 2
/// }
/// </summary>
/// <param name="Cities">Array od city comparison results (success i failures)</param>
/// <param name="ComparedAt">UTC timestamp kada je comparison operation initiated</param>
/// <param name="TotalCities">Total number od cities requested za comparison</param>
public record CityComparisonResponse(
    IReadOnlyList<CityComparisonEntry> Cities,
    DateTime ComparedAt,
    int TotalCities
);
