using System.Text.Json.Serialization;

namespace SarajevoAir.Api.Models;

/*
===========================================================================================
                                AQICN API RESPONSE MODELS
===========================================================================================

PURPOSE & EXTERNAL API INTEGRATION:
Model classes za deserialization od AQICN (World Air Quality Index) API responses.
Mapiraju JSON strukturu od aqicn.org web service-a na C# objects.

JSON DESERIALIZATION STRATEGY:
[JsonPropertyName] attributes mapiraju JSON property names na C# properties
Handles different naming conventions between JSON (snake_case) i C# (PascalCase)
Nullable properties handle optional/missing fields u API responses gracefully

API RESPONSE STRUCTURE OVERVIEW:
┌─────────────────────┐    ┌──────────────────────────┐    ┌─────────────────────┐
│   AQICN API         │────│    JSON RESPONSE         │────│   C# MODEL CLASSES   │
│  (aqicn.org)        │    │   (Raw HTTP Content)     │    │   (This File)        │  
└─────────────────────┘    └──────────────────────────┘    └─────────────────────┘
         │                              │                              │
         │                              ▼                              │
         │               ┌─────────────────────┐                      │
         │               │ System.Text.Json    │ ◄────────────────────┘
         │               │  Deserializer       │
         │               └─────────────────────┘
         │                              │
         │                              ▼
         └────────────────► ┌─────────────────────┐
                            │   BUSINESS LOGIC    │
                            │   (Services Layer)  │
                            └─────────────────────┘

EXTERNAL API DEPENDENCY:
These models directly mirror AQICN API structure
Changes u external API may require model updates
Versioning strategy important za backward compatibility
*/

/*
=== ROOT AQICN API RESPONSE ===

TOP-LEVEL RESPONSE CONTAINER:
All AQICN API calls return this standardized wrapper structure
Status field indicates success/error state
Data field contains actual air quality information
*/

/// <summary>
/// Root response wrapper za all AQICN API calls
/// Contains status indicator i optional data payload
/// 
/// Example JSON structure:
/// {
///   "status": "ok",
///   "data": { ... air quality data ... }
/// }
/// </summary>
public class AqicnResponse
{
    /*
    === API STATUS FIELD ===
    
    RESPONSE STATE INDICATOR:
    "ok" = Successful API call sa valid data
    "error" = Failed API call, check error message
    Other values possible depending on AQICN API versions
    */
    
    /// <summary>
    /// API response status - "ok" za success, "error" za failures
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    /*
    === MAIN DATA PAYLOAD ===
    
    CONDITIONAL DATA PRESENCE:
    Null when status != "ok" ili if API error occurs
    Contains complete air quality information when successful
    Nullable to handle error response scenarios gracefully
    */
    
    /// <summary>
    /// Main air quality data payload - null u case od API errors
    /// </summary>
    [JsonPropertyName("data")]
    public AqicnData? Data { get; set; }
}

/*
=== CORE AIR QUALITY DATA ===

COMPREHENSIVE AQI INFORMATION:
Contains all available air quality metrics od AQICN monitoring station
Includes current readings, forecasts, i metadata about monitoring location
*/

/// <summary>
/// Core air quality data container sa all measurement i metadata
/// </summary>
public class AqicnData
{
    /*
    === OVERALL AQI VALUE ===
    
    EPA AIR QUALITY INDEX:
    Primary metric - single number representing overall air quality
    Range: 0-500+ (higher = worse air quality)
    Calculated from worst pollutant at monitoring station
    */
    
    /// <summary>
    /// Overall Air Quality Index value (EPA scale 0-500+)
    /// </summary>
    [JsonPropertyName("aqi")]
    public int Aqi { get; set; }
    
    /*
    === STATION IDENTIFIER ===
    
    AQICN INTERNAL ID:
    Unique identifier za monitoring station u AQICN system
    Used za station-specific API calls i data correlation
    */
    
    /// <summary>
    /// AQICN internal station identifier
    /// </summary>
    [JsonPropertyName("idx")]
    public int Idx { get; set; }
    
    /*
    === LOCATION INFORMATION ===
    
    GEOGRAPHIC CONTEXT:
    City information including coordinates i station details
    Essential za mapping i location-based services
    */
    
    /// <summary>
    /// City i location information za monitoring station
    /// </summary>
    [JsonPropertyName("city")]
    public AqicnCity? City { get; set; }
    
    /*
    === DOMINANT POLLUTANT ===
    
    PRIMARY AIR QUALITY DRIVER:
    Identifies which pollutant is causing highest AQI reading
    Key za health advisory recommendations
    Common values: "pm25", "pm10", "o3", "no2", "so2", "co"
    
    NOTE: Typo u AQICN API - "dominentpol" instead od "dominantpol"
    */
    
    /// <summary>
    /// Primary pollutant driving current AQI value
    /// Note: AQICN API uses "dominentpol" (typo u their API)
    /// </summary>
    [JsonPropertyName("dominentpol")]
    public string DominentPol { get; set; } = string.Empty;
    
    /*
    === INDIVIDUAL AIR QUALITY MEASUREMENTS ===
    
    DETAILED POLLUTANT READINGS:
    Individual measurements za each monitored pollutant
    Enables detailed analysis beyond overall AQI
    */
    
    /// <summary>
    /// Individual air quality measurements za each pollutant
    /// </summary>
    [JsonPropertyName("iaqi")]
    public AqicnIaqi? Iaqi { get; set; }
    
    /*
    === TEMPORAL METADATA ===
    
    MEASUREMENT TIMESTAMPS:
    When data was collected i last updated
    Critical za data freshness assessment
    */
    
    /// <summary>
    /// Timestamp information za when data was collected
    /// </summary>
    [JsonPropertyName("time")]
    public AqicnTime? Time { get; set; }
    
    /*
    === PREDICTION DATA ===
    
    FUTURE AQI FORECASTS:
    Optional forecast data when available za station
    Not all stations provide forecast information
    */
    
    /// <summary>
    /// Optional forecast predictions za future air quality
    /// </summary>
    [JsonPropertyName("forecast")]
    public AqicnForecast? Forecast { get; set; }
    
    /*
    === DATA SOURCE ATTRIBUTION ===
    
    MONITORING NETWORK CREDITS:
    Information about organizations providing measurement data
    Important za data transparency i credibility
    */
    
    /// <summary>
    /// Array od organizations providing measurement data
    /// </summary>
    [JsonPropertyName("attributions")]
    public AqicnAttribution[]? Attributions { get; set; }
}

/*
=== MONITORING STATION LOCATION ===

GEOGRAPHIC i NETWORK INFORMATION:
Details about monitoring station location i web presence
Enables mapping functionality i source verification
*/

/// <summary>
/// Geographic i network information za air quality monitoring station
/// </summary>
public class AqicnCity
{
    /*
    === STATION NAME ===
    
    LOCATION IDENTIFIER:
    Human-readable name za monitoring station
    Often includes city name i specific location details
    Example: "Sarajevo, Otoka"
    */
    
    /// <summary>
    /// Human-readable monitoring station name i location
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /*
    === COORDINATES ===
    
    WGS84 GEOGRAPHIC COORDINATES:
    Array sa [latitude, longitude] u decimal degrees
    Used za mapping i distance calculations
    Null if coordinates not available
    */
    
    /// <summary>
    /// Geographic coordinates [latitude, longitude] u WGS84 system
    /// </summary>
    [JsonPropertyName("geo")]
    public double[]? Geo { get; set; }
    
    /*
    === AQICN STATION URL ===
    
    DIRECT LINK TO STATION:
    URL to station-specific page na aqicn.org
    Enables users to verify data directly sa source
    */
    
    /// <summary>
    /// Direct URL to station page na aqicn.org website
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
}

public class AqicnIaqi
{
    [JsonPropertyName("pm25")]
    public AqicnMeasurement? Pm25 { get; set; }
    
    [JsonPropertyName("pm10")]
    public AqicnMeasurement? Pm10 { get; set; }
    
    [JsonPropertyName("o3")]
    public AqicnMeasurement? O3 { get; set; }
    
    [JsonPropertyName("no2")]
    public AqicnMeasurement? No2 { get; set; }
    
    [JsonPropertyName("so2")]
    public AqicnMeasurement? So2 { get; set; }
    
    [JsonPropertyName("co")]
    public AqicnMeasurement? Co { get; set; }
    
    [JsonPropertyName("t")]
    public AqicnMeasurement? Temperature { get; set; }
    
    [JsonPropertyName("h")]
    public AqicnMeasurement? Humidity { get; set; }
    
    [JsonPropertyName("p")]
    public AqicnMeasurement? Pressure { get; set; }
    
    [JsonPropertyName("w")]
    public AqicnMeasurement? Wind { get; set; }
    
    [JsonPropertyName("dew")]
    public AqicnMeasurement? Dew { get; set; }
}

public class AqicnMeasurement
{
    [JsonPropertyName("v")]
    public double V { get; set; }
}

public class AqicnTime
{
    [JsonPropertyName("s")]
    public string S { get; set; } = string.Empty;
    
    [JsonPropertyName("tz")]
    public string Tz { get; set; } = string.Empty;
    
    [JsonPropertyName("v")]
    public long V { get; set; }
    
    [JsonPropertyName("iso")]
    public string Iso { get; set; } = string.Empty;
}

public class AqicnForecast
{
    [JsonPropertyName("daily")]
    public AqicnDailyForecast? Daily { get; set; }
}

public class AqicnDailyForecast
{
    [JsonPropertyName("pm25")]
    public AqicnDayForecast[]? Pm25 { get; set; }
    
    [JsonPropertyName("pm10")]
    public AqicnDayForecast[]? Pm10 { get; set; }
    
    [JsonPropertyName("o3")]
    public AqicnDayForecast[]? O3 { get; set; }
    
    [JsonPropertyName("uvi")]
    public AqicnDayForecast[]? Uvi { get; set; }
}

public class AqicnDayForecast
{
    [JsonPropertyName("avg")]
    public int Avg { get; set; }
    
    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;
    
    [JsonPropertyName("max")]
    public int Max { get; set; }
    
    [JsonPropertyName("min")]
    public int Min { get; set; }
}

public class AqicnAttribution
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}