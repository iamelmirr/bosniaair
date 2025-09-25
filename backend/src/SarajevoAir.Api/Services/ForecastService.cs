/*
=== FORECASTSERVICE.CS ===
AIR QUALITY FORECAST SERVICE

ARCHITECTURAL ROLE:
- Future air quality prediction i planning support
- Weather-based AQI forecasting data
- Extended cache strategy (2-hour TTL) zbog forecast stability
- Planning i advisory functionality za users

BUSINESS VALUE:
1. Planning outdoor activities (when will air be cleaner?)
2. Health-sensitive users preparation
3. Public health advisory system
4. Environmental awareness campaigns

DESIGN PATTERNS:
1. Forecast Cache Pattern - Longer TTL zbog predictive data stability
2. Data Transformation Pattern - API forecast → UI-friendly format
3. Graceful Degradation - Empty forecast better than service failure
4. Predictive Service Pattern - Future-oriented data processing
*/

using Microsoft.Extensions.Logging;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Models;

namespace SarajevoAir.Api.Services;

/*
=== FORECAST SERVICE INTERFACE ===

INTERFACE DESIGN:
- Single forecast method sa standard patterns
- Same city/forceFresh parameters kao other services
- Focused na predictive air quality data
- Immutable forecast response model

FORECAST SCOPE:
Multi-day air quality predictions based na weather patterns
Enables proactive decision making za users
*/

/// <summary>
/// Service interface za air quality forecasting operations  
/// Provides predictive AQI data za planning i advisory purposes
/// </summary>
public interface IForecastService
{
    /// <summary>
    /// Retrieves multi-day air quality forecast sa intelligent caching
    /// </summary>
    /// <param name="city">Target city za forecast (defaults to Sarajevo)</param>
    /// <param name="forceFresh">Bypass cache i force fresh API call</param>
    /// <param name="cancellationToken">Cancellation token support</param>
    /// <returns>Multi-day forecast response</returns>
    Task<ForecastResponse> GetForecastAsync(string city, bool forceFresh = false, CancellationToken cancellationToken = default);
}

/*
=== FORECAST SERVICE IMPLEMENTATION ===

PREDICTIVE DATA SPECIALIZATION:
Focuses na future air quality predictions rather than current readings
Extended caching strategy reflects forecast data stability

FORECAST CHARACTERISTICS:
- Weather-dependent predictions
- Multi-day outlook (typically 3-5 days)
- Less volatile than live readings
- Planning-oriented information
*/

/// <summary>
/// Implementation od air quality forecast processing logic
/// Specializes u predictive data sa extended caching strategy
/// </summary>
public class ForecastService : IForecastService
{
    /*
    === SERVICE DEPENDENCIES ===
    
    PREDICTIVE DATA ARCHITECTURE:
    _aqicnClient: External forecast API integration
    _cache: Extended TTL cache za forecast stability
    _logger: Forecast operation tracking
    
    FORECAST-SPECIFIC CONCERNS:
    Forecasts change less frequently than live data
    Weather patterns provide predictable air quality trends
    Longer caching reduces API calls i improves performance
    */
    private readonly IAqicnClient _aqicnClient;        // Forecast API client
    private readonly AirQualityCache _cache;           // Extended TTL caching
    private readonly ILogger<ForecastService> _logger; // Forecast logging

    /*
    === FORECAST CACHE CONFIGURATION ===
    
    EXTENDED TTL STRATEGY:
    2 hours TTL vs 10 minutes za live data
    Forecast data stability justifies longer caching
    
    BUSINESS RATIONALE:
    - Weather patterns change gradually
    - Forecast models update less frequently
    - API costs i rate limiting considerations
    - Better user experience sa stable predictions
    
    CACHE INVALIDATION:
    Time-based expiration only (no manual invalidation)
    forceFresh parameter allows cache bypass when needed
    */
    private static readonly TimeSpan ForecastTtl = TimeSpan.FromHours(2);

    /*
    === CONSTRUCTOR SA FORECAST DEPENDENCIES ===
    
    SPECIALIZED DEPENDENCY SET:
    No database dependency - forecasts aren't persisted historically
    Cache i API client sufficient za forecast operations
    */
    
    /// <summary>
    /// Constructor prima forecast-specific service dependencies
    /// </summary>
    public ForecastService(
        IAqicnClient aqicnClient,           // External forecast API
        AirQualityCache cache,              // Extended TTL caching
        ILogger<ForecastService> logger)    // Operation logging
    {
        _aqicnClient = aqicnClient;
        _cache = cache;
        _logger = logger;
    }

    /*
    === MAIN FORECAST RETRIEVAL METHOD ===
    
    FORECAST PROCESSING PIPELINE:
    1. Input normalization (standard city fallback)
    2. Extended cache strategy (2-hour TTL)
    3. API forecast data extraction
    4. Forecast transformation i validation
    5. Cache population sa extended TTL
    6. Response generation
    
    FORECAST vs LIVE DATA DIFFERENCES:
    - Longer cache TTL (2h vs 10min)
    - Forecast-specific API data parsing
    - Multi-day data processing
    - Planning-oriented response format
    
    ERROR HANDLING:
    Empty forecast response better than service failure
    Graceful degradation za missing forecast data
    */
    
    /// <summary>
    /// Main forecast retrieval method sa extended caching strategy
    /// </summary>
    public async Task<ForecastResponse> GetForecastAsync(string city, bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        /*
        === STANDARD INPUT NORMALIZATION ===
        Consistent sa other services - Sarajevo default fallback
        */
        var targetCity = string.IsNullOrWhiteSpace(city) ? "Sarajevo" : city.Trim();

        /*
        === EXTENDED CACHE STRATEGY ===
        
        FORECAST-SPECIFIC CACHING:
        TryGetForecast uses forecast cache entries (separate od live cache)
        2-hour TTL reflects forecast data stability
        
        CACHE HIT BENEFITS:
        Eliminates API calls za stable forecast data
        Improves response time za planning queries
        Reduces API rate limiting concerns
        
        SARAJEVO-ONLY CACHING:
        Same pattern kao live data - only primary city cached
        Other cities bypass cache (lower volume)
        */
        if (!forceFresh && targetCity.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase) &&
            _cache.TryGetForecast(targetCity, ForecastTtl, out var cacheEntry))
        {
            _logger.LogDebug("Returning cached forecast for {City}", targetCity);
            return cacheEntry.Response;
        }

        /*
        === FORECAST API DATA RETRIEVAL ===
        
        SAME API CLIENT:
        AqicnClient handles both live i forecast data
        Single API call retrieves comprehensive city information
        
        NESTED DATA STRUCTURE:
        apiResponse.Data.Forecast.Daily contains forecast array
        More complex than live data - requires careful null checking
        */
        var apiResponse = await _aqicnClient.GetCityDataAsync(targetCity, cancellationToken);
        if (apiResponse?.Data?.Forecast?.Daily is null)
        {
            /*
            === GRACEFUL FORECAST DEGRADATION ===
            
            MISSING DATA SCENARIOS:
            - API doesn't provide forecast za requested city
            - Weather model unavailable za region
            - Temporary API service issues
            
            EMPTY RESPONSE STRATEGY:
            Return empty forecast rather than null ili exception
            Enables UI to show "no forecast available" message
            Better user experience than service failure
            
            LOGGING:
            Warning level za operational awareness
            May indicate API changes ili regional limitations
            */
            _logger.LogWarning("Forecast data missing for {City}", targetCity);
            return new ForecastResponse(targetCity, Array.Empty<ForecastDayDto>(), DateTime.UtcNow);
        }

        /*
        === FORECAST DATA TRANSFORMATION ===
        
        BUILD PROCESS:
        BuildForecast transforms raw API forecast data u UI-friendly DTOs
        Handles multiple pollutant forecasts i date alignment
        
        FORECAST RESPONSE:
        Comprehensive response sa metadata i timestamp
        Immutable record type za thread safety
        */
        var forecast = BuildForecast(apiResponse.Data.Forecast.Daily, targetCity);
        var response = new ForecastResponse(targetCity, forecast, DateTime.UtcNow);
        
        /*
        === FORECAST CACHE POPULATION ===
        
        EXTENDED TTL CACHING:
        SetForecast stores response sa 2-hour expiration
        Separate forecast cache namespace
        Optimized za forecast data characteristics
        */
        _cache.SetForecast(targetCity, new AirQualityCache.ForecastEntry(response));
        return response;
    }

    /*
    === FORECAST DATA TRANSFORMATION METHOD ===
    
    PURPOSE:
    Transforms raw WAQI forecast API data u structured UI-friendly DTOs
    Handles multi-pollutant forecast alignment i range calculations
    
    WAQI FORECAST STRUCTURE:
    "forecast": {
      "daily": {
        "pm25": [{"day":"2025-01-03", "avg":35, "min":20, "max":50}],
        "pm10": [{"day":"2025-01-03", "avg":45, "min":25, "max":60}],
        "o3": [{"day":"2025-01-03", "avg":85, "min":60, "max":110}]
      }
    }
    
    DATA PROCESSING CHALLENGES:
    1. Pollutants have separate arrays (alignment needed)
    2. Date matching između different pollutant predictions  
    3. Range data (min/avg/max) vs single values
    4. Missing pollutants za some dates
    5. Variable forecast length (up to 7 days)
    */
    
    /// <summary>
    /// Transforms raw WAQI forecast data u structured daily forecast DTOs
    /// Handles pollutant alignment i range calculations
    /// </summary>
    private static IReadOnlyList<ForecastDayDto> BuildForecast(AqicnDailyForecast dailyForecast, string city)
    {
        /*
        === RESULT COLLECTION ===
        Accumulator za processed daily forecast entries
        */
        var days = new List<ForecastDayDto>();
        
        /*
        === PRIMARY POLLUTANT STRATEGY ===
        
        PM2.5 AS PRIMARY:
        PM2.5 drives forecast iteration (most important zdravstvo indicator)
        Other pollutants aligned sa PM2.5 dates
        
        NULL SAFETY:
        dailyForecast.Pm25 može biti null - fallback na empty array
        Prevents enumeration exceptions
        
        FORECAST LENGTH:
        Take(7) limits forecast na 7 days maximum
        API može provide variable lengths - standardize output
        */
        var pm25 = dailyForecast.Pm25 ?? Array.Empty<AqicnDayForecast>();

        /*
        === DAILY FORECAST ITERATION ===
        
        PM2.5 DRIVEN PROCESSING:
        Each PM2.5 forecast entry drives one forecast day
        Date alignment sa other pollutants based na PM2.5 dates
        
        MULTI-POLLUTANT ALIGNMENT:
        FirstOrDefault finds matching dates u other pollutant arrays
        Handles cases where some pollutants missing za specific dates
        */
        foreach (var entry in pm25.Take(7))  // Max 7 days forecast
        {
            /*
            === DATE ALIGNMENT ===
            
            COMMON DATE:
            entry.Day represents forecast date (ISO format)
            Used za matching napříč different pollutant arrays
            */
            var date = entry.Day;
            
            /*
            === AQI CALCULATION ===
            
            PM2.5 TO AQI CONVERSION:
            CalculateAqiFromPm25 converts PM2.5 average u AQI scale
            Uses EPA conversion formula za standardized health indication
            
            PRIMARY HEALTH INDICATOR:
            PM2.5 AQI often represents overall air quality level
            Other pollutants provide supplementary information
            */
            var aqi = CalculateAqiFromPm25(entry.Avg);
            
            /*
            === POLLUTANT ALIGNMENT STRATEGY ===
            
            DATE MATCHING:
            FirstOrDefault finds pollutant forecast za same date
            Returns null if no matching date found (graceful handling)
            
            CROSS-POLLUTANT CONSISTENCY:
            Ensures all pollutants aligned sa same forecast day
            Handles APIs that provide inconsistent pollutant coverage
            */
            var pm10 = dailyForecast.Pm10?.FirstOrDefault(x => x.Day == date);
            var o3 = dailyForecast.O3?.FirstOrDefault(x => x.Day == date);

            /*
            === DTO CONSTRUCTION ===
            
            COMPREHENSIVE FORECAST ENTRY:
            - Date: ISO date za client parsing
            - AQI: Overall air quality index  
            - Category: EPA classification za user guidance
            - Color: UI visualization support
            - Pollutants: Detailed range data za each pollutant
            
            RANGE DATA HANDLING:
            PollutantRangeDto captures min/avg/max forecast ranges
            Null handling za missing pollutant data
            Rich information za planning purposes
            */
            days.Add(new ForecastDayDto(
                Date: date,                            // ISO forecast date
                Aqi: aqi,                             // Calculated AQI from PM2.5
                Category: GetAqiCategory(aqi),        // EPA classification
                Color: GetAqiColor(aqi),              // UI color coding
                Pollutants: new ForecastDayPollutants(
                    Pm25: new PollutantRangeDto(entry.Avg, entry.Min, entry.Max),          // PM2.5 ranges
                    Pm10: pm10 is null ? null : new PollutantRangeDto(pm10.Avg, pm10.Min, pm10.Max), // PM10 ranges (optional)
                    O3: o3 is null ? null : new PollutantRangeDto(o3.Avg, o3.Min, o3.Max)           // O3 ranges (optional)
                )
            ));
        }

        return days;  // Immutable collection za public consumption
    }

    /*
    === PM2.5 TO AQI CONVERSION UTILITY ===
    
    PURPOSE:
    Converts PM2.5 concentration (µg/m³) u EPA AQI scale (0-500+)
    Critical za health interpretation od raw sensor data
    
    EPA AQI FORMULA IMPLEMENTATION:
    Uses official EPA breakpoint calculations
    Linear interpolation između defined concentration ranges
    
    PM2.5 BREAKPOINT RANGES (24-hour average):
    0.0-12.0 µg/m³     → 0-50 AQI (Good)
    12.1-35.4 µg/m³    → 51-100 AQI (Moderate)
    35.5-55.4 µg/m³    → 101-150 AQI (Unhealthy for Sensitive Groups)
    55.5-150.4 µg/m³   → 151-200 AQI (Unhealthy)
    150.5-250.4 µg/m³  → 201-300 AQI (Very Unhealthy)
    250.5-500.4 µg/m³  → 301-500 AQI (Hazardous)
    
    LINEAR INTERPOLATION FORMULA:
    AQI = ((AQI_hi - AQI_lo) / (PM_hi - PM_lo)) * (PM - PM_lo) + AQI_lo
    */
    
    /// <summary>
    /// Converts PM2.5 concentration u EPA AQI scale using official breakpoints
    /// </summary>
    private static int CalculateAqiFromPm25(double pm25)
    {
        /*
        RANGE 1: 0.0-12.0 µg/m³ → 0-50 AQI (Good)
        Formula: (50/12.0) * pm25
        */
        if (pm25 <= 12.0) return (int)Math.Round((50.0 / 12.0) * pm25);
        
        /*
        RANGE 2: 12.1-35.4 µg/m³ → 51-100 AQI (Moderate)
        Linear interpolation između breakpoints
        */
        if (pm25 <= 35.4) return (int)Math.Round(((100 - 51) / (35.4 - 12.1)) * (pm25 - 12.1) + 51);
        
        /*
        RANGE 3: 35.5-55.4 µg/m³ → 101-150 AQI (Unhealthy for Sensitive Groups)
        */
        if (pm25 <= 55.4) return (int)Math.Round(((150 - 101) / (55.4 - 35.5)) * (pm25 - 35.5) + 101);
        
        /*
        RANGE 4: 55.5-150.4 µg/m³ → 151-200 AQI (Unhealthy)
        */
        if (pm25 <= 150.4) return (int)Math.Round(((200 - 151) / (150.4 - 55.5)) * (pm25 - 55.5) + 151);
        
        /*
        RANGE 5: 150.5-250.4 µg/m³ → 201-300 AQI (Very Unhealthy)
        */
        if (pm25 <= 250.4) return (int)Math.Round(((300 - 201) / (250.4 - 150.5)) * (pm25 - 150.5) + 201);
        
        /*
        RANGE 6: 250.5+ µg/m³ → 301-500+ AQI (Hazardous)
        Beyond this range = extremely dangerous conditions
        */
        return (int)Math.Round(((500 - 301) / (500.4 - 250.5)) * (pm25 - 250.5) + 301);
    }

    /*
    === SHARED AQI UTILITY METHODS ===
    
    NOTE: These methods are duplicated napříč services
    Future refactoring opportunity: Extract u shared utility class
    
    BUSINESS CONSISTENCY:
    Identical implementations ensure consistent categorization
    Same color schemes napříč all service responses
    */

    /*
    === EPA STANDARD AQI CATEGORIES ===
    Same ranges kao u AirQualityService za consistency
    */
    
    /// <summary>
    /// Maps AQI value na EPA standard category classification
    /// </summary>
    private static string GetAqiCategory(int aqi) => aqi switch
    {
        <= 50 => "Good",                           // 0-50: Minimal health risk
        <= 100 => "Moderate",                      // 51-100: Minor concerns  
        <= 150 => "Unhealthy for Sensitive Groups", // 101-150: Sensitive groups affected
        <= 200 => "Unhealthy",                     // 151-200: General population affected
        <= 300 => "Very Unhealthy",               // 201-300: Emergency conditions
        _ => "Hazardous"                           // 301+: Health alert
    };

    /*
    === UI COLOR SCHEME ===
    
    TAILWIND CSS COLORS:
    Updated za modern UI color palette
    Different od other services - potentially inconsistent
    Should be standardized u future refactoring
    
    COLOR MAPPING:
    - #22C55E: Green-500 (Good)
    - #EAB308: Yellow-500 (Moderate)
    - #F97316: Orange-500 (Unhealthy for Sensitive Groups)
    - #EF4444: Red-500 (Unhealthy)
    - #A855F7: Purple-500 (Very Unhealthy)
    - #7C2D12: Brown-800 (Hazardous)
    */
    
    /// <summary>
    /// Returns modern Tailwind CSS color codes za AQI visualization
    /// </summary>
    private static string GetAqiColor(int aqi) => aqi switch
    {
        <= 50 => "#22C55E",   // Green-500 - Good air quality
        <= 100 => "#EAB308",  // Yellow-500 - Moderate air quality
        <= 150 => "#F97316",  // Orange-500 - Unhealthy for sensitive groups
        <= 200 => "#EF4444",  // Red-500 - Unhealthy
        <= 300 => "#A855F7",  // Purple-500 - Very unhealthy
        _ => "#7C2D12"        // Brown-800 - Hazardous
    };
}

/*
=== FORECASTSERVICE CLASS SUMMARY ===

ARCHITECTURAL OVERVIEW:
Predictive air quality service specialized za multi-day forecasting
Extended caching strategy optimized za forecast data characteristics

KEY DESIGN PATTERNS:
1. Extended Cache Pattern - 2-hour TTL za stable forecast data
2. Multi-Pollutant Alignment - Date-based cross-pollutant matching
3. PM2.5 Primary Strategy - Most important pollutant drives forecasting
4. Range Data Processing - Min/avg/max forecast ranges
5. Graceful Degradation - Empty responses za missing data

BUSINESS VALUE:
- Planning outdoor activities based na predicted air quality
- Health-sensitive users preparation i decision support
- Public health advisory za upcoming weather patterns
- Environmental awareness i trend prediction

FORECAST PROCESSING:
- 7-day maximum forecast window (manageable planning horizon)
- PM2.5-driven iteration (most health-relevant pollutant)
- Cross-pollutant date alignment (comprehensive daily view)
- EPA AQI conversion za standardized health interpretation
- Range data preservation (planning flexibility)

PERFORMANCE CHARACTERISTICS:
- Extended 2-hour cache TTL (forecast stability)
- Single API call za comprehensive forecast data
- Efficient date alignment algorithms
- Minimal data transformation overhead

INTEGRATION POINTS:
- ForecastController: HTTP request handling
- AqicnClient: External forecast API integration
- AirQualityCache: Extended TTL caching strategy
- Frontend planning components: Rich forecast visualization

FUTURE ENHANCEMENTS:
1. Configurable forecast length (3/7/14 days)
2. Weather integration (wind, humidity, temperature)
3. Accuracy tracking i model validation
4. Regional forecast comparison
5. Alert system za predicted poor air quality
6. Historical forecast accuracy metrics

CODE QUALITY CONSIDERATIONS:
1. Duplicate utility methods napříč services (refactor opportunity)
2. PM2.5 calculation could be extracted u shared utility
3. Color scheme inconsistency sa other services
4. Complex nested null checking u API response handling
*/
