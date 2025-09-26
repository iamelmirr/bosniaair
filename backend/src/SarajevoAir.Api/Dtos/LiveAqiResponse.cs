namespace SarajevoAir.Api.Dtos;

/*
===========================================================================================
                                  LIVE AQI DATA TRANSFER OBJECTS
===========================================================================================

PURPOSE & API CONTRACT DESIGN:
DTOs define structured API response contracts za live air quality data.
Immutable record types ensure data integrity i consistent serialization.

JSON SERIALIZATION OPTIMIZATION:
Records provide built-in JSON serialization support
Property names automatically converted to camelCase za JavaScript clients
Immutable structure prevents accidental data mutation during processing

DTO DESIGN PRINCIPLES:
1. Immutability - Records prevent data tampering after creation
2. Value Semantics - Equality based on property values, not references  
3. Minimal Data - Only essential properties za client consumption
4. Type Safety - Strong typing prevents runtime serialization errors
5. Null Safety - Explicit nullable properties za optional data
*/

/*
=== GEOGRAPHIC COORDINATE SYSTEM ===

SPATIAL DATA REPRESENTATION:
Standard WGS84 coordinate system za global compatibility
Used za mapping integrations i location-based services
*/

/// <summary>
/// Geographic coordinates za monitoring station location
/// Uses WGS84 coordinate system compatible sa mapping services
/// </summary>
/// <param name="Latitude">North-South position (-90 to +90 degrees)</param>
/// <param name="Longitude">East-West position (-180 to +180 degrees)</param>
public record CoordinateDto(double Latitude, double Longitude);

/*
=== INDIVIDUAL POLLUTANT MEASUREMENT ===

GRANULAR MEASUREMENT DATA:
Represents single pollutant reading od specific monitoring station
Enables detailed analysis od individual air quality components
*/

/// <summary>
/// Individual pollutant measurement od air quality monitoring station
/// Contains raw measurement data sa metadata za traceability
/// </summary>
/// <param name="Id">Unique measurement identifier od WAQI system</param>
/// <param name="City">City name where measurement was taken</param>
/// <param name="LocationName">Specific monitoring station location/name</param>
/// <param name="Parameter">Pollutant type (PM2.5, PM10, O3, NO2, SO2, CO)</param>
/// <param name="Value">Measured concentration value</param>
/// <param name="Unit">Unit od measurement (μg/m³, mg/m³, ppm)</param>
/// <param name="Timestamp">UTC timestamp kada je measurement recorded</param>
/// <param name="SourceName">Data source organization (typically WAQI)</param>
/// <param name="Coordinates">Optional geographic location od monitoring station</param>
/// <param name="AveragingPeriod">Optional time period over which measurement was averaged</param>
public record MeasurementDto(
    string Id,
    string City,
    string LocationName,
    string Parameter,
    double Value,
    string Unit,
    DateTime Timestamp,
    string SourceName,
    CoordinateDto? Coordinates = null,
    AveragingPeriodDto? AveragingPeriod = null
);

/*
=== MEASUREMENT AVERAGING METADATA ===

TEMPORAL AGGREGATION INFO:
Indicates time window za measurement averaging (1-hour, 24-hour, etc.)
Important za understanding data quality i reliability
*/

/// <summary>
/// Time period over which air quality measurement was averaged
/// Provides context za measurement reliability i comparison
/// </summary>
/// <param name="Value">Duration value (e.g., 1, 24)</param>
/// <param name="Unit">Time unit (hour, minute, day)</param>
public record AveragingPeriodDto(double Value, string Unit);

/*
=== PRIMARY LIVE AQI RESPONSE ===

MAIN API RESPONSE STRUCTURE:
Aggregated air quality information ready za client consumption
Combines EPA AQI calculations sa raw measurements za comprehensive view

CLIENT USAGE PATTERNS:
- OverallAqi: Primary display value za users
- AqiCategory/Color: Visual indicators za UI/UX
- HealthMessage: Actionable advice za users
- Measurements: Detailed pollutant breakdown za advanced users
- DominantPollutant: Identifies primary air quality concern
*/

/// <summary>
/// Complete live air quality response za specified city
/// Provides EPA AQI calculations sa detailed pollutant measurements
/// 
/// JSON Response Example:
/// {
///   "city": "Sarajevo",
///   "overallAqi": 87,
///   "aqiCategory": "Moderate", 
///   "color": "#FFFF00",
///   "healthMessage": "Air quality is acceptable for most people...",
///   "timestamp": "2024-03-15T14:30:00Z",
///   "measurements": [...],
///   "dominantPollutant": "PM2.5"
/// }
/// </summary>
/// <param name="City">Target city name</param>
/// <param name="OverallAqi">EPA AQI index value (0-500 scale)</param>
/// <param name="AqiCategory">EPA category (Good, Moderate, Unhealthy, etc.)</param>
/// <param name="Color">Hex color code za visual indicators</param>
/// <param name="HealthMessage">User-friendly health recommendation</param>
/// <param name="Timestamp">UTC timestamp od data collection</param>
/// <param name="Measurements">Detailed pollutant measurements array</param>
/// <param name="DominantPollutant">Primary pollutant driving AQI value</param>
public record LiveAqiResponse(
    string City = "",
    int OverallAqi = 0,
    string AqiCategory = "",
    string Color = "",
    string HealthMessage = "",
    DateTime Timestamp = default,
    IReadOnlyList<MeasurementDto> Measurements = null!,
    string DominantPollutant = ""
);
