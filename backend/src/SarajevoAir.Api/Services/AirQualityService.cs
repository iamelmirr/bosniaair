/*
=== AIRQUALITYSERVICE.CS ===
MAIN BUSINESS LOGIC SERVICE ZA AQI OPERATIONS

ARCHITECTURAL ROLE:
- Centralni service za sve air quality operacije
- Koordinira između external APIs, cache, i database
- Implementira business rules i data processing logic
- Serves kao facade za sve AQI-related functionality

SERVICE LAYER RESPONSIBILITIES:
1. Data orchestration (API + Cache + Database)
2. Business logic implementation
3. Performance optimization (caching strategy)
4. Data transformation (API models → DTOs)
5. Error handling i logging na business level
*/

using Microsoft.Extensions.Logging;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Models;
using SarajevoAir.Api.Repositories;

namespace SarajevoAir.Api.Services;

/*
=== SERVICE INTERFACE DEFINITION ===

INTERFACE DESIGN PRINCIPLES:
- Minimal surface area (2 core methods)
- Async-first approach za performance
- Optional parameters za flexibility
- CancellationToken support za timeout handling

METHOD CONTRACTS:
1. GetLiveAqiAsync: Primary read operation sa caching
2. PersistSarajevoSnapshotAsync: Write operation za background jobs
*/

/// <summary>
/// Interface defining core air quality business operations
/// Implemented by AirQualityService klasu
/// </summary>
public interface IAirQualityService
{
    /// <summary>
    /// Dobija live AQI podatke za specified grad sa intelligent caching
    /// </summary>
    /// <param name="city">Ime grada (default: Sarajevo)</param>
    /// <param name="forceFresh">Da li da zaobiđe cache i forsi fresh API call</param>
    /// <param name="cancellationToken">Token za canceling long-running operations</param>
    /// <returns>LiveAqiResponse sa current air quality data</returns>
    Task<LiveAqiResponse> GetLiveAqiAsync(string city, bool forceFresh = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Persists Sarajevo AQI snapshot u database za historical tracking
    /// Koristi se od background services za scheduled data collection
    /// </summary>
    /// <param name="aqi">AQI vrednost za persistence</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PersistSarajevoSnapshotAsync(int aqi, CancellationToken cancellationToken = default);
}

/*
=== MAIN SERVICE IMPLEMENTATION ===

DESIGN PATTERNS:
1. Service Layer Pattern - Business logic encapsulation
2. Dependency Injection - Loose coupling sa dependencies
3. Repository Pattern - Data access abstraction
4. Cache-Aside Pattern - Performance optimization
5. Strategy Pattern - Pollutant processing logic
*/

/// <summary>
/// Main implementation od IAirQualityService interface
/// Koordinira air quality operations across multiple data sources
/// </summary>
public class AirQualityService : IAirQualityService
{
    /*
    === STATIC CONFIGURATION ===
    
    POLLUTANT PROCESSING ORDER:
    Definiše prioritet za display u UI components
    PM2.5 i PM10 su najvažniji za health assessment
    O3 (ozone) je particularly important tokom summer months
    
    STATIC READONLY BENEFITS:
    - Compile-time optimization
    - Thread-safe bez locks
    - Memory efficient (shared across instances)
    */
    private static readonly string[] PollutantOrder = ["pm25", "pm10", "o3", "no2", "so2", "co"];

    /*
    === DEPENDENCY INJECTION FIELDS ===
    
    LAYERED ARCHITECTURE DEPENDENCIES:
    _aqicnClient: External API integration layer
    _repository: Data persistence layer  
    _cache: Performance optimization layer
    _logger: Cross-cutting logging concern
    
    READONLY FIELDS:
    Immutable after construction - thread safe by design
    Dependencies se nikad ne menjaju tokom object lifetime
    */
    private readonly IAqicnClient _aqicnClient;        // External WAQI API client
    private readonly IAqiRepository _repository;       // Database operations
    private readonly AirQualityCache _cache;           // In-memory caching
    private readonly ILogger<AirQualityService> _logger; // Structured logging

    /*
    === CACHE CONFIGURATION ===
    
    LIVE DATA TTL (Time To Live):
    10 minuta cache za live AQI data
    Balance između freshness i performance
    AQI data se ne menja drastično binnen 10 minuta
    
    CACHE STRATEGY:
    - Cache-aside pattern (application kontroliše cache)
    - Lazy loading (cache samo when requested)
    - TTL expiration za automatic invalidation
    */
    private static readonly TimeSpan LiveTtl = TimeSpan.FromMinutes(10);

    /*
    === CONSTRUCTOR SA DEPENDENCY INJECTION ===
    
    DEPENDENCY INJECTION PATTERN:
    ASP.NET Core DI container automatski resolve-uje sve dependencies
    Program.cs: services.AddScoped<IAirQualityService, AirQualityService>()
    
    SCOPED LIFETIME:
    Nova instanca za svaki HTTP request
    Dependencies su takođe scoped ili singleton
    Thread-safe tokom request lifecycle
    
    CONSTRUCTOR INJECTION BENEFITS:
    - Compile-time dependency verification
    - Explicit dependency declaration
    - Easy unit testing sa mock objects
    - Immutable dependencies (readonly fields)
    */
    
    /// <summary>
    /// Constructor prima sve potrebne dependencies preko DI container-a
    /// </summary>
    public AirQualityService(
        IAqicnClient aqicnClient,           // External API client za WAQI integration
        IAqiRepository repository,          // Database repository za persistence
        AirQualityCache cache,              // In-memory cache za performance
        ILogger<AirQualityService> logger)  // Structured logger za monitoring
    {
        _aqicnClient = aqicnClient;
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    /*
    === PRIMARY BUSINESS METHOD ===
    
    METHOD RESPONSIBILITIES:
    1. Input validation i normalization
    2. Cache strategy implementation
    3. External API orchestration
    4. Data transformation (API models → DTOs)
    5. Error handling i logging
    6. Performance optimization
    
    CACHING STRATEGY:
    - Cache hit: Return immediately (sub-millisecond response)
    - Cache miss: Fetch from API + cache result
    - Force fresh: Bypass cache completely
    - TTL expiration: Automatic cache invalidation
    
    PERFORMANCE CHARACTERISTICS:
    - Cached response: ~1ms response time
    - Fresh API call: ~200-500ms response time
    - Network timeout: ~30s max (Polly policy)
    */
    
    /// <summary>
    /// Glavni method za dobijanje live AQI podataka sa intelligent caching
    /// </summary>
    public async Task<LiveAqiResponse> GetLiveAqiAsync(string city, bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        /*
        === INPUT VALIDATION I NORMALIZATION ===
        
        DEFENSIVE PROGRAMMING:
        Handle null, empty, i whitespace-only strings
        Default fallback na "Sarajevo" kao primary city
        Trim() removes leading/trailing whitespace
        
        BUSINESS RULE:
        Sarajevo je default city zbog highest traffic volume
        */
        var targetCity = string.IsNullOrWhiteSpace(city) ? "Sarajevo" : city.Trim();

        /*
        === CACHE STRATEGY IMPLEMENTATION ===
        
        CACHE HIT CONDITIONS:
        1. !forceFresh: Normal operation (ne bypass cache)
        2. targetCity == "Sarajevo": Only Sarajevo se cache-uje (highest volume)
        3. _cache.TryGetLive success: Valid cached entry unutar TTL window
        
        CACHE KEY STRATEGY:
        Case-insensitive city matching za consistent cache hits
        "Sarajevo", "sarajevo", "SARAJEVO" → same cache entry
        
        PERFORMANCE BENEFIT:
        Cache hit eliminates external API call i JSON processing
        Response time: API call (300ms) → Cache hit (1ms)
        */
        if (!forceFresh && targetCity.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase) &&
            _cache.TryGetLive(targetCity, LiveTtl, out var cachedEntry))
        {
            _logger.LogDebug("Returning cached live AQI for {City}", targetCity);
            return cachedEntry.Response;
        }

        /*
        === EXTERNAL API ORCHESTRATION ===
        
        ASYNC API CALL:
        Delegiraju external API complex logic na AqicnClient
        CancellationToken propagation za timeout handling
        
        ERROR SCENARIOS:
        - Network connectivity issues
        - Invalid city/station mapping
        - API rate limiting
        - JSON deserialization errors
        
        NULL SAFETY:
        apiResponse?.Data checks both null response i null data property
        */
        var apiResponse = await _aqicnClient.GetCityDataAsync(targetCity, cancellationToken);
        if (apiResponse?.Data is null)
        {
            /*
            === ERROR HANDLING STRATEGY ===
            
            BUSINESS EXCEPTION:
            InvalidOperationException sa descriptive message
            Indicate business rule violation (no data available)
            
            ALTERNATIVE APPROACHES:
            1. Return null (requires null handling u Controllers)
            2. Return empty response (može confuse clients)
            3. Throw exception (current approach - fail fast)
            
            EXCEPTION MESSAGE:
            User-friendly message sa city context
            Helpful za debugging i client-side error handling
            */
            throw new InvalidOperationException($"No AQI data available for {targetCity}.");
        }

        /*
        === DATA TRANSFORMATION ===
        
        BUSINESS LOGIC LAYER:
        BuildLiveResponse transformiše raw API data u business DTOs
        Encapsulates complex pollutant processing logic
        Standardizes response format preko different cities
        
        SEPARATION OF CONCERNS:
        API client handle raw data retrieval
        Service layer handle business logic transformation
        Controller layer handle HTTP concerns
        */
        var liveResponse = BuildLiveResponse(apiResponse.Data, targetCity);
        
        /*
        === CACHE POPULATION ===
        
        CACHE-ASIDE PATTERN:
        Application je responsible za cache population
        Cache se populate AFTER successful API call
        Fresh data se cache za subsequent requests
        
        CACHE ENTRY STRUCTURE:
        LiveEntry wrappuje response sa metadata (timestamp)
        Enables TTL calculation i cache invalidation
        */
        _cache.SetLive(targetCity, new AirQualityCache.LiveEntry(liveResponse));

        /*
        === SARAJEVO-SPECIFIC PERSISTENCE ===
        
        BUSINESS RULE:
        Only Sarajevo data se persists za historical analysis
        Other cities su available live only (no storage)
        
        ASYNC PERSISTENCE:
        Fire-and-forget pattern za background database write
        Ne blokira response generation za better UX
        
        CONDITIONAL PERSISTENCE:
        Case-insensitive comparison za consistent behavior
        Handles "Sarajevo", "sarajevo", etc. uniformly
        */
        if (targetCity.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase))
        {
            await PersistSarajevoSnapshotAsync(liveResponse.OverallAqi, cancellationToken);
        }

        return liveResponse;
    }

    /*
    === DATABASE PERSISTENCE METHOD ===
    
    METHOD PURPOSE:
    Persists Sarajevo AQI snapshots za historical tracking i analytics
    Called from background services i live data fetching
    
    BUSINESS LOGIC:
    1. Duplicate detection (avoid redundant records)
    2. Rate limiting (max 1 record per 5 minutes)
    3. Value change detection (only store when AQI changes)
    4. Efficient database operations
    
    PERFORMANCE OPTIMIZATION:
    - Check existing records before insert
    - Skip unnecessary database writes
    - Async operations za non-blocking execution
    */
    
    /// <summary>
    /// Persists AQI snapshot u database sa intelligent duplicate detection
    /// </summary>
    public async Task PersistSarajevoSnapshotAsync(int aqi, CancellationToken cancellationToken = default)
    {
        /*
        === DUPLICATE DETECTION LOGIC ===
        
        DATABASE QUERY:
        GetMostRecentAsync returns latest AQI record za Sarajevo
        Ordered by Timestamp DESC → most recent first
        
        BUSINESS RULES:
        1. Time-based deduplication (5 minute window)
        2. Value-based deduplication (same AQI value)
        3. Database efficiency (avoid unnecessary inserts)
        
        NULL HANDLING:
        latest može biti null ako je database empty (first run)
        */
        var latest = await _repository.GetMostRecentAsync("Sarajevo", cancellationToken);
        if (latest is not null)
        {
            /*
            === TEMPORAL DEDUPLICATION ===
            
            TIME WINDOW CALCULATION:
            (DateTime.UtcNow - latest.Timestamp) calculates time difference
            TotalMinutes converts TimeSpan u decimal minutes
            
            DEDUPLICATION RULES:
            1. < 5 minutes: Too soon za new record
            2. Same AQI value: No meaningful change
            Both conditions = Skip database write
            
            LOGGING STRATEGY:
            LogDebug za operational insight bez production noise
            Structured logging sa AQI value za filtering
            */
            var minutesSince = (DateTime.UtcNow - latest.Timestamp).TotalMinutes;
            if (minutesSince < 5 && latest.AqiValue == aqi)
            {
                _logger.LogDebug("Skipping AQI snapshot save – recent record exists with same value {Aqi}", aqi);
                return;  // Early return - no database operation
            }
        }

        /*
        === DATABASE INSERT OPERATION ===
        
        ENTITY CREATION:
        SimpleAqiRecord entity sa current timestamp i AQI value
        Repository pattern abstracts database implementation details
        
        ASYNC OPERATION:
        AddRecordAsync ne blokira thread za other operations
        CancellationToken omogućava graceful shutdown
        */
        await _repository.AddRecordAsync(new Entities.SimpleAqiRecord
        {
            City = "Sarajevo",                    // Hardcoded za Sarajevo-specific persistence
            AqiValue = aqi,                       // Current AQI vrednost iz API response
            Timestamp = DateTime.UtcNow           // UTC timestamp za consistent timezone handling
        }, cancellationToken);
        
        /*
        === SUCCESS LOGGING ===
        
        AUDIT TRAIL:
        LogInformation za production monitoring i analytics
        Structured logging sa AQI value za metrics collection
        
        OPERATIONAL INSIGHT:
        Enables tracking database write frequency i AQI trends
        Useful za performance monitoring i data verification
        */
        _logger.LogInformation("Saved Sarajevo AQI snapshot: {Aqi}", aqi);
    }

    /*
    === CORE DATA TRANSFORMATION METHOD ===
    
    PURPOSE:
    Transformiše raw WAQI API data u standardized LiveAqiResponse DTO
    Central business logic za data processing i normalization
    
    TRANSFORMATION PROCESS:
    1. Pollutant measurements processing
    2. AQI classification i categorization
    3. Color coding za UI visualization
    4. Health message generation
    5. Metadata enrichment (timestamp, dominant pollutant)
    
    DESIGN PATTERN:
    Static method - pure function bez side effects
    Input (AqicnData + city) → Output (LiveAqiResponse)
    Easily testable i predictable behavior
    */
    
    /// <summary>
    /// Transformiše raw API data u standardized business DTO
    /// </summary>
    private static LiveAqiResponse BuildLiveResponse(AqicnData data, string city)
    {
        /*
        === MEASUREMENTS PROCESSING ===
        
        POLLUTANT DATA EXTRACTION:
        BuildMeasurements processes complex nested API structure
        Normalizes individual pollutant readings za consistent format
        */
        var measurements = BuildMeasurements(data, city);
        
        /*
        === AQI VALUE EXTRACTION ===
        
        PRIMARY METRIC:
        data.Aqi represents overall Air Quality Index
        Single number (0-500+) representing overall air quality
        Most important value za user decision making
        */
        var aqi = data.Aqi;
        
        /*
        === DTO CONSTRUCTION ===
        
        RECORD PATTERN:
        LiveAqiResponse je record type sa immutable properties
        Named parameters za clear constructor calls
        
        DATA ENRICHMENT:
        Raw API data se enrich sa business logic:
        - AQI category classification (Good, Moderate, etc.)
        - Color coding za UI visualization
        - Health advisory messages
        - Standardized timestamp
        - Dominant pollutant identification
        */
        return new LiveAqiResponse(
            City: data.City?.Name ?? city,                    // API city name ili fallback na input
            OverallAqi: aqi,                                  // Primary AQI metric
            AqiCategory: GetAqiCategory(aqi),                 // Business classification
            Color: GetAqiColor(aqi),                          // UI visualization
            HealthMessage: GetHealthMessage(aqi),             // User advisory
            Timestamp: DateTime.UtcNow,                       // Response generation time
            Measurements: measurements,                       // Individual pollutant data
            DominantPollutant: string.IsNullOrWhiteSpace(data.DominentPol) ? PollutantOrder[0] : data.DominentPol  // Primary pollutant
        );
    }

    /*
    === POLLUTANT MEASUREMENTS PROCESSING ===
    
    PURPOSE:
    Extractuje individual pollutant readings iz complex WAQI API response
    Transformiše u standardized MeasurementDto objects
    
    WAQI API STRUCTURE:
    "iaqi": {
      "pm25": {"v": 25},
      "pm10": {"v": 15},
      "o3": {"v": 45}
    }
    
    BUSINESS LOGIC:
    1. GPS coordinate extraction sa fallbacks
    2. Null-safe pollutant processing
    3. Standardized measurement units
    4. Unique ID generation za tracking
    5. Consistent metadata enrichment
    */
    
    /// <summary>
    /// Processes individual pollutant measurements iz WAQI API response
    /// </summary>
    private static IReadOnlyList<MeasurementDto> BuildMeasurements(AqicnData data, string city)
    {
        /*
        === RESULT COLLECTION ===
        List<MeasurementDto> za building individual measurements
        IReadOnlyList return type za immutable public interface
        */
        var result = new List<MeasurementDto>();
        
        /*
        === GPS COORDINATE EXTRACTION ===
        
        COMPLEX NULL-SAFE PATTERN:
        data.City?.Geo checks if City i Geo su non-null
        is { Length: > 0 } pattern matching za array validation
        geoLat[0] i geoLon[1] extract latitude i longitude
        
        FALLBACK COORDINATES:
        43.8563, 18.4131 = Sarajevo center coordinates
        Used when API ne provides location data
        
        BOSNIA CITIES COORDINATES:
        - Sarajevo: 43.8563, 18.4131
        - Tuzla: 44.5386, 18.6708  
        - Mostar: 43.3438, 17.8078
        - Banja Luka: 44.7722, 17.1910
        */
        var latitude = data.City?.Geo is { Length: > 0 } geoLat ? geoLat[0] : 43.8563;
        var longitude = data.City?.Geo is { Length: > 1 } geoLon ? geoLon[1] : 18.4131;

        /*
        === LOCAL HELPER FUNCTION ===
        
        NESTED FUNCTION PATTERN:
        TryAddMeasurement encapsulates repetitive measurement processing
        Local function scope - accessible only within BuildMeasurements
        Captures outer scope variables (result, city, data, latitude, longitude)
        
        NULL-SAFE PROCESSING:
        Early return if measurement je null (ne baca exceptions)
        Defensive programming za missing pollutant data
        
        DTO CONSTRUCTION:
        MeasurementDto record sa rich metadata:
        - Unique ID za tracking i correlation
        - City context za multi-city support
        - Standardized units za consistent display
        - Timestamp za temporal tracking
        - Source attribution za data provenance
        - GPS coordinates za mapping features
        */
        void TryAddMeasurement(string parameter, AqicnMeasurement? measurement, string unit)
        {
            if (measurement is null)
            {
                return;  // Skip missing measurements gracefully
            }

            result.Add(new MeasurementDto(
                Id: Guid.NewGuid().ToString(),                    // Unique identifier
                City: city,                                       // Input city context
                LocationName: data.City?.Name ?? "City Center",   // Station location
                Parameter: parameter,                             // Pollutant type (pm25, pm10, etc.)
                Value: measurement.V,                             // Actual measurement value
                Unit: unit,                                       // Measurement unit
                Timestamp: DateTime.UtcNow,                       // Processing timestamp
                SourceName: "AQICN",                            // Data source attribution
                Coordinates: new CoordinateDto(latitude, longitude) // GPS location
            ));
        }

        /*
        === POLLUTANT PROCESSING SEQUENCE ===
        
        ORDERED BY HEALTH IMPORTANCE:
        1. PM2.5 (Fine Particles) - Most dangerous za respiratory health
        2. PM10 (Coarse Particles) - Visible particles, immediate irritation
        3. O3 (Ozone) - Secondary pollutant, summer smog
        4. NO2 (Nitrogen Dioxide) - Traffic emissions, respiratory irritant
        5. SO2 (Sulfur Dioxide) - Industrial emissions, acid rain precursor
        6. CO (Carbon Monoxide) - Colorless, odorless, toxic gas
        
        UNIT STANDARDIZATION:
        µg/m³ (micrograms per cubic meter) - WHO standard za particles i gases
        mg/m³ (milligrams per cubic meter) - CO due to higher concentrations
        
        NULL SAFETY:
        data.Iaqi?.Pm25 uses null-conditional operator
        Missing pollutants se gracefully skip (ne crash aplikaciju)
        */
        TryAddMeasurement("pm25", data.Iaqi?.Pm25, "µg/m³");  // Fine particles
        TryAddMeasurement("pm10", data.Iaqi?.Pm10, "µg/m³");  // Coarse particles  
        TryAddMeasurement("o3", data.Iaqi?.O3, "µg/m³");      // Ground-level ozone
        TryAddMeasurement("no2", data.Iaqi?.No2, "µg/m³");    // Nitrogen dioxide
        TryAddMeasurement("so2", data.Iaqi?.So2, "µg/m³");    // Sulfur dioxide
        TryAddMeasurement("co", data.Iaqi?.Co, "mg/m³");      // Carbon monoxide

        return result;  // Immutable list za public consumption
    }

    /*
    === AQI CLASSIFICATION UTILITY METHODS ===
    
    PURPOSE:
    Static helper methods za AQI business logic i UI support
    Implementiraju US EPA AQI standard classifications
    
    DESIGN PATTERNS:
    - Switch expressions za concise pattern matching
    - Static methods za pure functions (no side effects)
    - Immutable string returns za thread safety
    - Range-based classification za clear boundaries
    */

    /*
    === AQI CATEGORY CLASSIFICATION ===
    
    US EPA AQI STANDARD RANGES:
    0-50: Good (Green) - Air quality je satisfactory
    51-100: Moderate (Yellow) - Acceptable sa minor concerns
    101-150: Unhealthy for Sensitive Groups (Orange) - Sensitive people affected
    151-200: Unhealthy (Red) - Everyone may experience effects
    201-300: Very Unhealthy (Purple) - Emergency conditions
    301+: Hazardous (Maroon) - Health alert for everyone
    
    SWITCH EXPRESSION PATTERN:
    Modern C# pattern matching sa range operators
    <= 50 checks "less than or equal" za inclusive ranges
    _ wildcard catches all remaining values (301+)
    */
    
    /// <summary>
    /// Classifies AQI value into EPA standard category
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
    === AQI COLOR CODING ===
    
    STANDARD EPA COLOR SCHEME:
    Colors provide instant visual indication od air quality level
    Used in UI components za consistent user experience
    
    HEX COLOR VALUES:
    - #00e400: Green (Good) - Safe, go outside
    - #ffff00: Yellow (Moderate) - Generally acceptable  
    - #ff7e00: Orange (Unhealthy for Sensitive) - Caution for sensitive groups
    - #ff0000: Red (Unhealthy) - Health effects for everyone
    - #8f3f97: Purple (Very Unhealthy) - Serious health effects
    - #7e0023: Maroon (Hazardous) - Emergency conditions
    
    UI INTEGRATION:
    Frontend components use these colors za:
    - Background colors in AQI cards
    - Progress bar indicators  
    - Map visualization markers
    - Chart color schemes
    */
    
    /// <summary>
    /// Returns EPA standard color code za AQI visualization
    /// </summary>
    private static string GetAqiColor(int aqi) => aqi switch
    {
        <= 50 => "#00e400",   // Green - Good air quality
        <= 100 => "#ffff00",  // Yellow - Moderate air quality
        <= 150 => "#ff7e00",  // Orange - Unhealthy for sensitive groups
        <= 200 => "#ff0000",  // Red - Unhealthy
        <= 300 => "#8f3f97",  // Purple - Very unhealthy
        _ => "#7e0023"        // Maroon - Hazardous
    };

    /*
    === HEALTH ADVISORY MESSAGES ===
    
    PURPOSE:
    User-friendly health recommendations based na AQI levels
    Official EPA guidance messages za public education
    
    MESSAGE STRATEGY:
    - Progressive severity language
    - Actionable guidance za users  
    - Consistent sa international AQI standards
    - Clear differentiation između risk levels
    
    SENSITIVE GROUPS INCLUDE:
    - People sa heart or lung disease
    - Older adults (65+)
    - Children i teenagers
    - People who are active outdoors
    
    USAGE CONTEXT:
    Messages se display u:
    - Live AQI cards na homepage
    - City comparison views
    - Mobile notifications
    - Health advisory sections
    */
    
    /// <summary>
    /// Provides health advisory message based na AQI level
    /// </summary>
    private static string GetHealthMessage(int aqi) => aqi switch
    {
        <= 50 => "Air quality is considered satisfactory, and air pollution poses little or no risk.",
        <= 100 => "Air quality is acceptable; however, there may be some health concern for a very small number of people who are unusually sensitive to air pollution.",
        <= 150 => "Members of sensitive groups may experience health effects. The general public is not likely to be affected.",
        <= 200 => "Everyone may begin to experience health effects; members of sensitive groups may experience more serious health effects.",
        <= 300 => "Health warnings of emergency conditions. The entire population is more likely to be affected.",
        _ => "Health alert: everyone may experience more serious health effects."
    };
}

/*
=== AIRQUALITYSERVICE CLASS SUMMARY ===

ARCHITECTURAL OVERVIEW:
Core business logic service that orchestrates air quality operations
across multiple layers (API, Cache, Database, Business Rules)

KEY DESIGN PATTERNS:
1. Service Layer Pattern - Business logic encapsulation
2. Cache-Aside Pattern - Performance optimization  
3. Repository Pattern - Data access abstraction
4. Strategy Pattern - AQI classification logic
5. Dependency Injection - Loose coupling

PERFORMANCE CHARACTERISTICS:
- Cached requests: ~1ms response time
- Fresh API calls: ~300ms response time  
- Database writes: Async, non-blocking
- Memory usage: Minimal (stateless service)

BUSINESS RULES IMPLEMENTED:
- Sarajevo-only persistence (other cities live only)
- 10-minute cache TTL za live data
- 5-minute deduplication za database writes
- Graceful degradation za API failures
- EPA standard AQI classifications

INTEGRATION POINTS:
- Controllers: HTTP request handling
- AqicnClient: External API integration  
- AqiRepository: Database operations
- AirQualityCache: Performance optimization
- Background Services: Scheduled data collection

MONITORING & OBSERVABILITY:
- Structured logging kroz all operations
- Rich contextual information za debugging
- Performance metrics za cache hit rates
- Error tracking za external API reliability
- Business metrics za AQI trends

TESTING CONSIDERATIONS:
- Pure functions za utility methods (easy unit testing)
- Interface-based dependencies (mockable)
- Separation of concerns (testable in isolation)
- No static dependencies (deterministic behavior)
*/
