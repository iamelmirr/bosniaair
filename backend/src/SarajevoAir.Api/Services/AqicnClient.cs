/*
=== WAQI (World Air Quality Index) API CLIENT ===

HIGH LEVEL OVERVIEW:
Ovaj servis je EXTERNAL API CLIENT koji se spaja na WAQI API (api.waqi.info)
za prikupljanje real-time podataka o kvalitetu vazduha po gradovima

BUSINESS PURPOSE:
- Dobija live AQI podatke sa specifičnih monitoring stanica  
- Konvertuje JSON response u tipizovane C# objekte
- Implementira error handling i retry logic
- Loguje sve API pozive za monitoring i debugging

DESIGN PATTERNS:
1. HTTP CLIENT PATTERN - tipizovani HttpClient sa DI
2. OPTIONS PATTERN - konfiguracija iz appsettings.json
3. RESILIENCE PATTERNS - Polly retry policies (konfigurisan u Program.cs)
4. STRUCTURED LOGGING - Serilog sa structured properties

INTEGRATION ARCHITECTURE:
AirQualityService → IAqicnClient → HttpClient → WAQI API → JSON → AqicnResponse
*/

using System.Text.Json;
using Microsoft.Extensions.Options;
using SarajevoAir.Api.Configuration;
using SarajevoAir.Api.Models;

namespace SarajevoAir.Api.Services;

/*
=== AQICN CLIENT INTERFACE ===
Definiše contract za komunikaciju sa WAQI API-jem
Service layer koristi ovaj interface umesto konkretne implementacije

DEPENDENCY INVERSION PRINCIPLE:
Business servisi zavise od abstrakcije (interface), ne od implementacije
Omogućava lako unit testing i mock-ovanje external API poziva
*/
public interface IAqicnClient
{
    /*
    === GET CITY DATA METHOD CONTRACT ===
    
    PARAMETERS:
    - city: ime grada (opciono - fallback na config.City)
    - cancellationToken: omogućava prekid HTTP request-a
    
    RETURN VALUE:
    - AqicnResponse? - nullable jer API poziv može da ne uspe
    - null znači da podaci nisu dostupni (error, network issue, itd.)
    
    BUSINESS SCENARIOS:
    1. Success: vraća AqicnResponse sa AQI podacima
    2. Invalid city: vraća null + log warning  
    3. Network error: vraća null + log error
    4. API error: vraća null + log warning
    */
    
    /// <summary>
    /// Dobija AQI podatke za određeni grad sa WAQI API-ja
    /// </summary>
    /// <param name="city">Ime grada (opciono - koristi se default iz config-a)</param>
    /// <param name="cancellationToken">Token za prekidanje HTTP request-a</param>
    /// <returns>AqicnResponse objekat ili null ako request nije uspeo</returns>
    Task<AqicnResponse?> GetCityDataAsync(string? city = null, CancellationToken cancellationToken = default);
}

/*
=== CONCRETE AQICN CLIENT IMPLEMENTATION ===
Implementira IAqicnClient interface koristeći HttpClient za stvarne HTTP pozive
Registrovan je kao typed HTTP client u Program.cs sa Polly resilience policies
*/
public class AqicnClient : IAqicnClient
{
    /*
    === DEPENDENCY INJECTION FIELDS ===
    Sve dependencies se injektuju kroz konstruktor iz DI container-a
    
    HTTPCLIENT:
    - Typed HTTP client registrovan u Program.cs
    - Automatski ima Polly retry policies (AddStandardResilienceHandler)
    - Reuses connections za performance (connection pooling)
    - Thread-safe za concurrent requests
    
    CONFIGURATION:
    - IOptions pattern za type-safe pristup appsettings.json
    - Automatic binding "Aqicn" sekcije na AqicnConfiguration klasu
    - Hot-reload support u development-u
    
    LOGGER:
    - Generic logger tipizovan za ovu klasu
    - Automatic correlation sa request context-om
    - Structured logging sa Serilog
    */
    private readonly HttpClient _httpClient;           // HTTP client sa resilience policies
    private readonly AqicnConfiguration _config;       // API configuration (URL, token, default city)
    private readonly ILogger<AqicnClient> _logger;     // Structured logger za ovu klasu

    /*
    === CONSTRUCTOR SA DEPENDENCY INJECTION ===
    
    TYPED HTTP CLIENT PATTERN:
    Program.cs: AddHttpClient<IAqicnClient, AqicnClient>()
    DI automatski kreira HttpClient instancu samo za ovu klasu
    
    OPTIONS PATTERN:
    config.Value dobija trenutnu konfiguraciju iz appsettings.json
    
    GENERIC LOGGER:
    ILogger<AqicnClient> automatski dodaje class name u sve log entries
    */
    
    /// <summary>
    /// Konstruktor prima sve dependencies iz DI container-a
    /// </summary>
    /// <param name="httpClient">Typed HTTP client sa Polly resilience policies</param>
    /// <param name="config">WAQI API konfiguracija iz appsettings.json</param>
    /// <param name="logger">Structured logger za ovu klasu</param>
    public AqicnClient(HttpClient httpClient, IOptions<AqicnConfiguration> config, ILogger<AqicnClient> logger)
    {
        _httpClient = httpClient;      // Typed client sa connection pooling i resilience
        _config = config.Value;        // Current configuration snapshot
        _logger = logger;              // Generic logger sa class context
    }

    /*
    === MAIN API METHOD IMPLEMENTATION ===
    
    HIGH LEVEL FLOW:
    1. Parameter resolution (city name fallback)
    2. City-to-station mapping lookup
    3. URL construction sa API token
    4. HTTP request execution
    5. JSON response parsing
    6. Response validation
    7. Structured logging kroz ceo process
    
    ERROR HANDLING STRATEGY:
    - Graceful degradation - vraća null umesto exception
    - Comprehensive logging za debugging
    - Different exception types za različite failure modes
    
    PERFORMANCE CONSIDERATIONS:
    - Async/await za non-blocking I/O
    - CancellationToken support za request timeouts
    - HttpClient connection reuse
    - Polly resilience patterns (retry, circuit breaker)
    */
    
    /// <summary>
    /// Implementacija metode za dobijanje AQI podataka sa WAQI API-ja
    /// </summary>
    public async Task<AqicnResponse?> GetCityDataAsync(string? city = null, CancellationToken cancellationToken = default)
    {
        try
        {
            /*
            === PARAMETER RESOLUTION ===
            Null coalescing operator (??) za fallback na default grad iz konfiguracije
            targetCity može biti:
            1. Eksplicitno prosleđen parametar (npr. "Tuzla")
            2. Default grad iz appsettings.json (obično "Sarajevo")
            */
            var targetCity = city ?? _config.City;
            
            /*
            === CITY-TO-STATION MAPPING LOOKUP ===
            
            WAQI API REQUIREMENT:
            API ne prima imena gradova nego specifične station ID-jeve
            Primer: "Tuzla" → "A462985", "Sarajevo" → "@9265"
            
            TRYGETVALUE PATTERN:
            Bezbedna dictionary lookup bez KeyNotFoundException
            Vraća false ako ključ ne postoji, true + out value ako postoji
            
            CASE INSENSITIVE LOOKUP:
            Dictionary je kreiran sa StringComparer.OrdinalIgnoreCase
            "Tuzla", "tuzla", "TUZLA" su svi validni ključevi
            */
            if (!AqicnConfiguration.CityStations.TryGetValue(targetCity, out var stationId))
            {
                // STRUCTURED LOGGING sa rich properties
                _logger.LogWarning("No station configured for city: {City}. Available cities: {Cities}", 
                    targetCity, string.Join(", ", AqicnConfiguration.CityStations.Keys));
                return null;  // Graceful failure - ne baca exception
            }
            
            /*
            === URL CONSTRUCTION ===
            
            WAQI API ENDPOINT FORMAT:
            https://api.waqi.info/feed/{stationId}/?token={apiToken}
            
            EXAMPLE URLs:
            - Sarajevo: https://api.waqi.info/feed/@9265/?token=xxx
            - Tuzla: https://api.waqi.info/feed/A462985/?token=xxx
            
            STRING INTERPOLATION:
            $"string {variable}" sintaksa za readable URL building
            */
            var url = $"{_config.ApiUrl}/feed/{stationId}/?token={_config.ApiToken}";
            
            /*
            === REQUEST INITIATION LOGGING ===
            Structured logging BEFORE HTTP request za audit trail
            Properties: {City} i {StationId} će biti indexable u log aggregation
            */
            _logger.LogInformation("Fetching air quality data from AQICN for city: {City} using station: {StationId}", targetCity, stationId);
            
            /*
            === HTTP REQUEST EXECUTION ===
            
            ASYNC HTTP CALL:
            GetAsync vraća Task<HttpResponseMessage>
            await oslobađa thread za druge request-e
            cancellationToken omogućava request timeout/cancel
            
            POLLY RESILIENCE:
            HttpClient je configured sa Polly policies u Program.cs:
            - Retry policy: 3 pokušaja sa exponential backoff
            - Circuit breaker: otvara circuit posle 5 consecutive failures
            - Timeout policy: 30s max za request
            
            CONNECTION REUSE:
            Typed HttpClient koristi HttpClientFactory
            Automatski connection pooling i DNS refresh
            */
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            /*
            === HTTP STATUS VALIDATION ===
            
            EnsureSuccessStatusCode() method:
            - Proverava da li je status code 2xx (200-299)
            - Baca HttpRequestException za non-success status
            - Alternative: response.IsSuccessStatusCode boolean property
            
            COMMON HTTP ERRORS:
            - 401 Unauthorized: invalid API token
            - 404 Not Found: nevaljan station ID
            - 429 Too Many Requests: rate limiting
            - 500 Internal Server Error: API problem
            */
            response.EnsureSuccessStatusCode();
            
            /*
            === JSON RESPONSE PROCESSING ===
            
            ReadAsStringAsync():
            - Čita HTTP response body kao string
            - Async metoda za large responses
            - cancellationToken za timeout support
            
            DEBUG LOGGING:
            Raw JSON response za development debugging
            LogDebug se neće logirati u production (LogLevel.Information+)
            Pažljivo sa sensitive data u logovima
            */
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            /*
            === JSON DESERIALIZATION CONFIGURATION ===
            
            PropertyNameCaseInsensitive = true:
            Omogućava flexible property matching
            "station_name", "stationName", "StationName" → svi mapiraju na StationName property
            
            ALTERNATIVE APPROACH:
            JsonNamingPolicy.CamelCase za strict camelCase conversion
            PropertyNameCaseInsensitive je fleksibilniji za external APIs
            */
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            /*
            === JSON DESERIALIZATION EXECUTION ===
            
            WAQI API JSON STRUCTURE:
            {
              "status": "ok",
              "data": {
                "aqi": 42,
                "iaqi": { "pm25": {"v": 25}, "pm10": {"v": 15} },
                "time": { "s": "2025-01-02 15:00:00", "tz": "+01:00" },
                "city": { "name": "Sarajevo, Bosnia and Herzegovina" }
              }
            }
            
            DESERIALIZATION PROCESS:
            - System.Text.Json mapira JSON properties na C# properties
            - PropertyNameCaseInsensitive hendluje različite naming conventions
            - Missing properties dobijaju default values (null, 0, etc.)
            
            ERROR SCENARIOS:
            - Malformed JSON: JsonException
            - Type mismatch: JsonException
            - Large JSON: OutOfMemoryException (retko)
            */
            var result = JsonSerializer.Deserialize<AqicnResponse>(jsonContent, options);
            
            /*
            === API RESPONSE VALIDATION ===
            
            WAQI API STATUS SEMANTICS:
            - "ok": successful response sa validnim podacima
            - "error": API level error (invalid token, station not found)
            - null: deserialization failure
            
            NULL CONDITIONAL OPERATORS:
            result?.Status koristi null-conditional za safe property access
            Sprečava NullReferenceException ako je result null
            
            BUSINESS LOGIC CHECK:
            Različito od HTTP status (že je već proverena)
            API može vratiti 200 OK sa status: "error"
            */
            if (result?.Status != "ok")
            {
                _logger.LogWarning("AQICN API returned error status: {Status}", result?.Status);
                return null;  // Graceful failure
            }
            
            /*
            === SUCCESS RESPONSE PROCESSING ===
            
            RICH LOGGING:
            Logiranje ključnih business metrics:
            - City name iz API response-a (može biti different od input)
            - AQI vrednost za quick monitoring
            
            NULL SAFE PROPERTY ACCESS:
            result?.Data?.City?.Name koristi chained null-conditional operators
            Bilo koji null u chain-u rezultuje u null finalnu vrednost
            
            AUDIT TRAIL:
            LogInformation za production monitoring i analytics
            Omogućava tracking API usage patterns i response characteristics
            */
            _logger.LogInformation("Successfully fetched air quality data for {City}. AQI: {Aqi}", 
                result?.Data?.City?.Name, result?.Data?.Aqi);
            
            return result;
        }
        
        /*
        === COMPREHENSIVE ERROR HANDLING ===
        
        EXCEPTION HIERARCHY:
        1. HttpRequestException: HTTP transport errors
        2. JsonException: JSON parsing errors  
        3. Exception: catch-all za unexpected errors
        
        ERROR HANDLING STRATEGY:
        - Log detailed error information za debugging
        - Return null za graceful degradation
        - Ne propagiraju exceptions dalje (defensive programming)
        */
        
        /*
        === HTTP REQUEST EXCEPTIONS ===
        
        COMMON SCENARIOS:
        - Network connectivity issues
        - DNS resolution failures
        - HTTP timeouts (pre-Polly timeout)
        - SSL/TLS certificate errors
        - HTTP error status codes (4xx, 5xx)
        
        LOGGING STRATEGY:
        LogError sa exception object za full stack trace
        Structured logging omogućava correlation sa other events
        */
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error when fetching data from AQICN API");
            return null;
        }
        
        /*
        === JSON DESERIALIZATION EXCEPTIONS ===
        
        COMMON SCENARIOS:
        - Malformed JSON response
        - API schema changes (missing/renamed properties)
        - Encoding issues (rare)
        - Large JSON exceeding memory limits
        
        DEBUGGING VALUE:
        Raw jsonContent je logged ranije kao LogDebug
        Exception message + stack trace daju detailed failure context
        */
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error when processing AQICN response");
            return null;
        }
        
        /*
        === CATCH-ALL EXCEPTION HANDLER ===
        
        DEFENSIVE PROGRAMMING:
        Hvata sve ostale unexpected exceptions
        Sprečava crash aplikacije zbog unforeseen errors
        
        POTENTIAL SCENARIOS:
        - OutOfMemoryException (large responses)
        - OperationCanceledException (timeout)
        - NullReferenceException (bugs in kodu)
        - ArgumentException (configuration issues)
        
        PRODUCTION RESILIENCE:
        Application će continue da radi čak i sa unexpected errors
        Svi errors su logged za post-mortem analysis
        */
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when fetching data from AQICN API");
            return null;
        }
    }
}

/*
=== SERVICE CLASS SUMMARY ===

DESIGN PATTERNS IMPLEMENTED:
1. Typed HTTP Client Pattern - encapsulated HTTP configuration
2. Options Pattern - strongly typed configuration injection
3. Graceful Degradation - null returns instead of exceptions
4. Structured Logging - rich contextual information
5. Async/Await - non-blocking I/O operations

ARCHITECTURE BENEFITS:
- Testable: Dependencies can be mocked
- Resilient: Multiple layers of error handling
- Observable: Comprehensive logging for monitoring
- Maintainable: Clear separation of concerns
- Performant: Async operations + HTTP client reuse

INTEGRATION POINTS:
- Called by AirQualityService (business logic layer)
- Uses AqicnConfiguration (configuration layer)
- Integrates with Polly policies (resilience layer)
- Produces logs for Serilog (observability layer)

WAQI API INTEGRATION:
- Handles city-to-station mapping
- Manages API authentication
- Processes complex nested JSON responses
- Provides fallback mechanisms for reliability
*/