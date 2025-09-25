using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

/*
===========================================================================================
                                    DAILY AQI TRENDS CONTROLLER
===========================================================================================

PURPOSE & ANALYTICS RESPONSIBILITY:
Historical AQI data analysis endpoint za trend identification i reporting.
Provides 7-day rolling statistics based on persisted measurement snapshots.

DATA SOURCE ARCHITECTURE:
Unlike live/forecast controllers koje call external APIs, daily controller
uses INTERNAL DATABASE as primary data source:

┌─────────────────────┐    ┌──────────────────────────┐    ┌─────────────────────┐
│   CLIENT REQUEST    │────│    DAILY CONTROLLER      │────│   DATABASE QUERIES   │
│  (Trend Analysis)   │    │   (Historical Data)      │    │  (Stored Snapshots)  │
└─────────────────────┘    └──────────────────────────┘    └─────────────────────┘
                                      │
                                      ▼
                           ┌─────────────────────┐
                           │ BACKGROUND SERVICE  │
                           │ Populates Database  │
                           │   Every 10 Min      │
                           └─────────────────────┘

ANALYTICAL VALUE:
- Air quality trend identification over time
- Peak pollution period analysis  
- Weekly pattern recognition za health planning
- Historical comparison za environmental monitoring
- Compliance reporting za regulatory authorities

PERFORMANCE CHARACTERISTICS:
- Database-only queries (no external API dependency)
- Fast response times (~5-20ms depending on data volume)
- Pre-aggregated data reduces computation overhead
- Suitable za dashboard i reporting applications
*/

/// <summary>
/// REST API controller za historical daily AQI statistics
/// Provides 7-day trend analysis based on database snapshots
/// </summary>
[ApiController]
[Route("api/v1/daily")]
public class DailyController : ControllerBase
{
    /*
    === DATABASE-FOCUSED DEPENDENCIES ===
    
    SERVICE SPECIALIZATION:
    IDailyAqiService - Handles database queries za historical data
    No external API dependencies, pure database analytics service
    */
    private readonly IDailyAqiService _dailyAqiService;
    private readonly ILogger<DailyController> _logger;

    /// <summary>
    /// Constructor za database analytics service injection
    /// </summary>
    public DailyController(IDailyAqiService dailyAqiService, ILogger<DailyController> logger)
    {
        _dailyAqiService = dailyAqiService;
        _logger = logger;
    }

    /*
    === HISTORICAL DATA ENDPOINT ===
    
    DATABASE ANALYTICS FOCUS:
    No external API calls - pure database query operation
    Returns aggregated 7-day trends sa statistical summaries
    
    DATA FRESHNESS:
    Data populated by background service every 10 minutes
    Last update timestamp included u responses
    Historical data immutable after storage
    */
    
    /// <summary>
    /// Retrieves 7-day historical AQI trends za specified city
    /// Returns daily aggregates sa min/max/avg statistics
    /// 
    /// Example requests:
    /// GET /api/v1/daily - Sarajevo 7-day trends (database only)
    /// GET /api/v1/daily?city=Sarajevo - Same as default
    /// 
    /// Response includes:
    /// - Daily AQI averages za last 7 days
    /// - Peak i lowest measurements per day
    /// - Trend direction indicators
    /// - Data freshness timestamps
    /// </summary>
    /// <param name="city">City name za historical analysis</param>
    /// <param name="cancellationToken">Database query cancellation</param>
    /// <returns>7-day AQI statistics sa trend indicators</returns>
    [HttpGet]
    public async Task<IActionResult> GetDailyAqi(
        [FromQuery] string city = "Sarajevo",
        CancellationToken cancellationToken = default)
    {
        try
        {
            /*
            === DATABASE QUERY EXECUTION ===
            
            PURE ANALYTICS OPERATION:
            No caching needed - database already optimized
            No external dependencies - reliable i fast
            Service handles EF Core query optimization
            */
            var response = await _dailyAqiService.GetDailyAqiAsync(city, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            /*
            === DATABASE ERROR HANDLING ===
            
            MINIMAL FAILURE SCENARIOS:
            Database connection issues (rare)
            Query timeout za large datasets (unlikely sa 7-day limit)
            Data corruption scenarios (very rare)
            
            SIMPLIFIED ERROR RESPONSE:
            Database errors easier to diagnose than API failures
            Less variability u error types
            */
            _logger.LogError(ex, "Failed to get daily AQI data for city {City}", city);
            return StatusCode(500, new
            {
                message = "Failed to retrieve daily AQI data",
                city
            });
        }
    }
}