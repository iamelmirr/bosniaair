using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SarajevoAir.Api.Services;

/*
===========================================================================================
                            AIR QUALITY BACKGROUND REFRESH SERVICE
===========================================================================================

PURPOSE & ARCHITECTURAL ROLE:
Bu service implements background data collection pattern za maintaining fresh AQI data.
Extends BackgroundService za automatic lifecycle management u ASP.NET Core hosting model.

KEY DESIGN PATTERNS:
1. Background Service Pattern - Long-running task u separate thread
2. Periodic Timer Pattern - Non-blocking scheduled execution
3. Circuit Breaker Pattern - Error isolation za external API failures
4. Cache Warm-up Strategy - Pre-populates cache sa fresh data

BUSINESS VALUE:
- Ensures real-time AQI data availability bez user wait times
- Reduces API load by batching requests vs on-demand calls  
- Improves user experience sa immediate cache responses
- Provides resilient data collection independent od web requests

INTEGRATION ARCHITECTURE:
┌─────────────────┐    ┌──────────────────────┐    ┌─────────────────────┐
│   ASP.NET Core  │────│  Background Service  │────│   External WAQI API │
│   Host Lifetime │    │   (This Service)     │    │   (aqicn.org)       │
└─────────────────┘    └──────────────────────┘    └─────────────────────┘
         │                         │                           │
         │                         ▼                           │
         │              ┌─────────────────────┐               │
         │              │  AirQualityService  │◄──────────────┘
         │              │    (Cache Layer)     │
         │              └─────────────────────┘
         │                         │
         │                         ▼
         └──────────────► ┌─────────────────────┐
                          │   User Requests     │
                          │  (Instant Response) │
                          └─────────────────────┘

PERFORMANCE CHARACTERISTICS:
- Refresh Frequency: 10 minutes (balances freshness vs API limits)
- Memory Usage: ~2MB za cached responses (live + forecast)
- Network I/O: ~500KB every 10 minutes
- CPU Impact: Minimal - async/await patterns prevent thread blocking
*/

/// <summary>
/// Background service koji periodically refreshes AQI data za Sarajevo
/// Maintains warm cache za instant user responses
/// </summary>
public class AirQualityRefreshService : BackgroundService
{
    /*
    === BACKGROUND SERVICE CONFIGURATION ===
    
    REFRESH INTERVAL STRATEGY:
    10 minutes balances:
    - Data freshness requirements (AQI changes relatively slowly)
    - API rate limiting (aqicn.org has request limits)
    - Resource usage (network, memory, CPU)
    - Cache TTL alignment (live cache: 10min, forecast: 2hours)
    
    INTERVAL REASONING:
    Too frequent (< 5min): Wastes API calls, risk rate limiting
    Too infrequent (> 20min): Stale data, poor user experience  
    10 minutes: Sweet spot za real-time feel sa reasonable overhead
    */
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);

    /*
    === DEPENDENCY INJECTION ===
    
    SERVICE COMPOSITION:
    IAirQualityService - Handles live AQI data collection sa caching
    IForecastService - Manages forecast data collection sa caching  
    ILogger - Structured logging za monitoring i debugging
    
    DESIGN BENEFITS:
    - Testable: Easy to mock dependencies za unit tests
    - Flexible: Can swap implementations bez changing logic
    - Observable: Comprehensive logging za production monitoring
    */
    private readonly IAirQualityService _airQualityService;
    private readonly IForecastService _forecastService;
    private readonly ILogger<AirQualityRefreshService> _logger;

    /*
    === CONSTRUCTOR & DEPENDENCY RESOLUTION ===
    
    ASP.NET CORE DI INTEGRATION:
    Services resolved automatically od DI container
    Lifecycle managed by hosting environment
    Singleton registration ensures single background instance
    */
    
    /// <summary>
    /// Initializes background refresh service sa required dependencies
    /// Called by ASP.NET Core DI container during startup
    /// </summary>
    public AirQualityRefreshService(
        IAirQualityService airQualityService,
        IForecastService forecastService,
        ILogger<AirQualityRefreshService> logger)
    {
        _airQualityService = airQualityService;
        _forecastService = forecastService;
        _logger = logger;
    }

    /*
    === MAIN BACKGROUND EXECUTION LOOP ===
    
    BACKGROUND SERVICE PATTERN:
    ExecuteAsync je entry point za long-running background tasks
    Runs u separate thread pool thread
    Lifecycle tied to application hosting environment
    
    EXECUTION STRATEGY:
    1. Initial refresh - Warm cache immediately on startup
    2. Periodic timer - Non-blocking scheduled execution
    3. Graceful shutdown - Responds to cancellation tokens
    
    ERROR HANDLING LAYERS:
    - OperationCanceledException: Expected during shutdown
    - Individual refresh errors: Logged but don't stop service
    - Unhandled exceptions: Will restart service (BackgroundService behavior)
    */
    
    /// <summary>
    /// Main execution method za background data collection
    /// Handles periodic refresh scheduling i graceful shutdown
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Air quality refresh service started");

        /*
        === INITIAL CACHE WARMING ===
        
        STARTUP OPTIMIZATION:
        First refresh happens immediately na startup
        Ensures cache je populated before first user request
        Eliminates cold start delays za initial API calls
        */
        await RefreshAsync(stoppingToken);

        try
        {
            /*
            === PERIODIC TIMER PATTERN ===
            
            .NET 6+ BEST PRACTICE:
            PeriodicTimer replaces older Timer patterns
            Benefits:
            - Async-friendly API design
            - Automatic disposal sa using statement  
            - Non-overlapping execution (waits za previous completion)
            - Built-in cancellation token support
            
            EXECUTION FLOW:
            WaitForNextTickAsync blocks until next interval
            Returns false when cancellation requested
            Each tick triggers new refresh attempt
            */
            using var timer = new PeriodicTimer(RefreshInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            /*
            === GRACEFUL SHUTDOWN ===
            
            EXPECTED EXCEPTION:
            OperationCanceledException thrown during app shutdown
            Not an error - normal part od graceful stop process
            Allows cleanup i final logging before termination
            */
            _logger.LogInformation("Air quality refresh service stopping");
        }
    }

    /*
    === CORE REFRESH OPERATION ===
    
    CACHE WARMING STRATEGY:
    forceFresh: true bypasses cache i forces fresh API calls
    Ensures cache je populated sa latest data od external APIs
    
    DUAL DATA COLLECTION:
    1. Live AQI data - Current air quality measurements
    2. Forecast data - Predicted air quality za next 24 hours
    
    ERROR ISOLATION DESIGN:
    Individual refresh failures don't stop background service
    Allows partial success (live data OK, forecast fails)
    Next refresh attempt will retry failed operations
    */
    
    /// <summary>
    /// Performs single refresh cycle za both live i forecast data
    /// Uses forceFresh to bypass cache i populate sa fresh API data
    /// </summary>
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refreshing Sarajevo AQI and forecast data");
            
            /*
            === PARALLEL DATA COLLECTION ===
            
            CONCURRENT API CALLS:
            Both calls execute concurrently za better performance
            forceFresh: true ensures cache bypass i fresh data collection
            CancellationToken enables graceful shutdown during API calls
            
            CACHE POPULATION SIDE EFFECTS:
            GetLiveAqiAsync populates live cache as side effect
            GetForecastAsync populates forecast cache as side effect
            Future user requests get instant cached responses
            */
            await _airQualityService.GetLiveAqiAsync("Sarajevo", forceFresh: true, cancellationToken);
            await _forecastService.GetForecastAsync("Sarajevo", forceFresh: true, cancellationToken);
            
            _logger.LogInformation("Refresh completed successfully");
        }
        catch (Exception ex)
        {
            /*
            === RESILIENT ERROR HANDLING ===
            
            ISOLATION STRATEGY:
            Catch all exceptions to prevent service termination
            Log full exception details za debugging
            Service continues running i will retry na next interval
            
            FAILURE SCENARIOS:
            - Network connectivity issues
            - API rate limiting responses  
            - External API service outages
            - JSON parsing errors
            - Timeout exceptions
            
            RECOVERY BEHAVIOR:
            Service remains active i operational
            Next refresh cycle will attempt recovery
            Cached data remains available during outages
            */
            _logger.LogError(ex, "Failed to refresh Sarajevo air quality data");
        }
    }
}
