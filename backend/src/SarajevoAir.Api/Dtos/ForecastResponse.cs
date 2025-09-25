namespace SarajevoAir.Api.Dtos;

/*
===========================================================================================
                              AIR QUALITY FORECAST DATA TRANSFER OBJECTS
===========================================================================================

PURPOSE & PREDICTIVE ANALYTICS:
DTOs za air quality predictions i future AQI estimates.
Enables planning i decision-making based on anticipated air conditions.

FORECAST DATA CHARACTERISTICS:
- 24-hour ahead predictions sa hourly granularity
- Statistical ranges (min/avg/max) za uncertainty representation
- Primary pollutants only (PM2.5, PM10, O3) za focused predictions
- Confidence intervals through range boundaries

TEMPORAL PREDICTION PATTERNS:
Forecast data inherently uncertain - ranges provide confidence bounds
Avg values za primary display, Min/Max za risk assessment
Shorter ranges indicate higher confidence u predictions
*/

/*
=== POLLUTANT PREDICTION RANGES ===

STATISTICAL UNCERTAINTY MODELING:
Air quality predictions contain inherent uncertainty
Range values help users understand confidence levels i plan accordingly
*/

/// <summary>
/// Statistical range za pollutant concentration predictions
/// Provides uncertainty bounds around forecasted values
/// </summary>
/// <param name="Avg">Average predicted concentration (primary display value)</param>
/// <param name="Min">Minimum expected concentration (best case scenario)</param>
/// <param name="Max">Maximum expected concentration (worst case scenario)</param>
public record PollutantRangeDto(int Avg, int Min, int Max);

/*
=== DAILY FORECAST PREDICTION ===

COMPREHENSIVE DAY-LEVEL FORECAST:
Complete air quality prediction za single day
Includes overall AQI i individual pollutant breakdowns
*/

/// <summary>
/// Complete air quality forecast za single day
/// Combines overall AQI prediction sa pollutant-specific ranges
/// </summary>
/// <param name="Date">Date za forecast u ISO format (YYYY-MM-DD)</param>
/// <param name="Aqi">Predicted overall AQI value (EPA scale 0-500)</param>
/// <param name="Category">EPA AQI category za predicted value</param>
/// <param name="Color">Hex color code za visual representation</param>
/// <param name="Pollutants">Individual pollutant prediction ranges</param>
public record ForecastDayDto(
    string Date,
    int Aqi,
    string Category,
    string Color,
    ForecastDayPollutants Pollutants
);

/*
=== POLLUTANT-SPECIFIC PREDICTIONS ===

FOCUSED PREDICTION SET:
Only major pollutants included u forecasts (PM2.5, PM10, O3)
Other pollutants (NO2, SO2, CO) typically not forecasted reliably
Nullable properties handle missing prediction data gracefully
*/

/// <summary>
/// Pollutant-specific forecast ranges za major air quality indicators
/// Only includes pollutants commonly available u prediction models
/// </summary>
/// <param name="Pm25">Fine particulate matter predictions (most health-relevant)</param>
/// <param name="Pm10">Coarse particulate matter predictions</param>
/// <param name="O3">Ground-level ozone predictions (photochemical smog)</param>
public record ForecastDayPollutants(
    PollutantRangeDto? Pm25,
    PollutantRangeDto? Pm10,
    PollutantRangeDto? O3
);

/*
=== PRIMARY FORECAST RESPONSE ===

MULTI-DAY PREDICTION CONTAINER:
Typically contains 1-5 days od predictions depending on API availability
Ordered chronologically za timeline presentation
*/

/// <summary>
/// Complete air quality forecast response za specified city
/// Contains multi-day predictions sa uncertainty ranges
/// 
/// JSON Response Example:
/// {
///   "city": "Sarajevo",
///   "forecast": [
///     {
///       "date": "2024-03-16",
///       "aqi": 95,
///       "category": "Moderate",
///       "color": "#FFFF00",
///       "pollutants": {
///         "pm25": { "avg": 35, "min": 28, "max": 42 },
///         "pm10": { "avg": 65, "min": 50, "max": 80 },
///         "o3": null
///       }
///     }
///   ],
///   "timestamp": "2024-03-15T14:30:00Z"
/// }
/// </summary>
/// <param name="City">Target city za forecast predictions</param>
/// <param name="Forecast">Chronologically ordered daily predictions</param>
/// <param name="Timestamp">UTC timestamp kada je forecast generated</param>
public record ForecastResponse(
    string City,
    IReadOnlyList<ForecastDayDto> Forecast,
    DateTime Timestamp
);
