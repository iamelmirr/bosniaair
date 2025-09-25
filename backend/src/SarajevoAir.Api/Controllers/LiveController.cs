using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

/*
===========================================================================================
                                    LIVE AQI DATA CONTROLLER  
===========================================================================================

PURPOSE & API RESPONSIBILITY:
Primary endpoint za real-time air quality data retrieval.
Handles GET requests za current AQI measurements sa caching optimization.

REST API DESIGN PATTERNS:
1. Resource-Based Routing - /api/v1/live represents live AQI resource
2. Query Parameter Filtering - ?city=Name enables city selection
3. HTTP Status Code Semantics - 200/404/500 za proper client communication
4. Content Negotiation - JSON responses za web/mobile clients

API CONTRACT DESIGN:
┌─────────────────────────┐    ┌──────────────────────────┐    ┌─────────────────────┐
│      HTTP CLIENT        │────│    LIVE CONTROLLER       │────│  AIR QUALITY SERVICE │
│   (Browser/Mobile)      │    │   (This Controller)      │    │   (Business Logic)   │
└─────────────────────────┘    └──────────────────────────┘    └─────────────────────┘
         │                              │                              │
         │ GET /api/v1/live?city=X      │                              │
         │ ──────────────────────────►  │ GetLiveAqiAsync(city)       │
         │                              │ ──────────────────────────► │
         │                              │                              │
         │ 200 OK + JSON Response       │ ◄────────────────────────── │
         │ ◄────────────────────────── │                              │

CACHING STRATEGY INTEGRATION:
- Sarajevo: Cache-first approach (background refresh maintains freshness)
- Other cities: Always fresh (lower usage, prevents stale data)
- Client-side: ETags/Cache-Control headers za browser optimization

PERFORMANCE CHARACTERISTICS:
- Cache hit (Sarajevo): ~1-2ms response time
- Fresh API call: ~200-500ms response time  
- Concurrent requests: Thread-safe via service layer
- Error resilience: Graceful degradation sa meaningful error responses
*/

/// <summary>
/// REST API controller za live air quality data endpoints
/// Optimized za high-frequency requests sa intelligent caching
/// </summary>
[ApiController]
[Route("api/v1/live")]
public class LiveController : ControllerBase
{
    /*
    === CONTROLLER DEPENDENCIES ===
    
    DEPENDENCY INJECTION PATTERN:
    IAirQualityService - Core business logic za AQI operations
    ILogger - Structured logging za monitoring i debugging
    
    CONTROLLER LIFECYCLE:
    Scoped lifetime - New instance per HTTP request
    Dependencies resolved od ASP.NET Core DI container
    Automatic disposal after request completion
    */
    private readonly IAirQualityService _airQualityService;
    private readonly ILogger<LiveController> _logger;

    /// <summary>
    /// Constructor za dependency injection setup
    /// ASP.NET Core resolves dependencies automatically
    /// </summary>
    public LiveController(IAirQualityService airQualityService, ILogger<LiveController> logger)
    {
        _airQualityService = airQualityService;
        _logger = logger;
    }

    /*
    === PRIMARY API ENDPOINT ===
    
    HTTP METHOD SELECTION:
    GET - Safe, idempotent operation za data retrieval
    No side effects, cacheable by browsers i proxies
    
    ROUTE TEMPLATE DESIGN:
    /api/v1/live - RESTful resource naming
    Query parameters za filtering i options
    
    PARAMETER BINDING STRATEGY:
    [FromQuery] - Binds URL query parameters to method parameters
    Default values enable flexible client usage
    CancellationToken supports request cancellation
    */
    
    /// <summary>
    /// Retrieves current air quality measurements za specified city
    /// Sarajevo data cached za performance, other cities always fresh
    /// 
    /// Example requests:
    /// GET /api/v1/live - Returns Sarajevo data (default, cached)
    /// GET /api/v1/live?city=Tuzla - Fresh data za Tuzla
    /// GET /api/v1/live?city=Sarajevo&refresh=true - Force fresh Sarajevo data
    /// </summary>
    /// <param name="city">City name za AQI data (default: Sarajevo)</param>
    /// <param name="refresh">Force fresh API call even za Sarajevo</param>
    /// <param name="cancellationToken">Enables request cancellation</param>
    /// <returns>Live AQI data sa measurements i health recommendations</returns>
    [HttpGet]
    public async Task<IActionResult> GetLiveData(
        [FromQuery] string city = "Sarajevo",
        [FromQuery] bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            /*
            === CACHING STRATEGY LOGIC ===
            
            INTELLIGENT CACHE BYPASS:
            forceFresh = true kada:
            1. Client explicitly requests refresh (refresh=true)
            2. City nije Sarajevo (other cities always fresh)
            
            REASONING:
            Sarajevo - Main city, high usage, cached za performance
            Other cities - Lower usage, fresh data ensures accuracy
            Refresh parameter - Enables cache busting when needed
            */
            var forceFresh = refresh || !city.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase);
            
            /*
            === SERVICE LAYER DELEGATION ===
            
            BUSINESS LOGIC SEPARATION:
            Controller handles HTTP concerns, service handles AQI logic
            CancellationToken passed through za async operation control
            Service layer manages caching, API calls, data transformation
            */
            var response = await _airQualityService.GetLiveAqiAsync(city, forceFresh, cancellationToken);
            
            /*
            === SUCCESS RESPONSE ===
            
            HTTP 200 OK sa JSON payload
            ASP.NET Core automatically serializes response object
            Content-Type: application/json header added automatically
            */
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            /*
            === BUSINESS LOGIC EXCEPTION HANDLING ===
            
            EXPECTED ERROR SCENARIO:
            InvalidOperationException indicates no data available za city
            Not a system error - valid business rule violation
            
            HTTP 404 NOT FOUND RESPONSE:
            Semantically correct - requested resource (city AQI) doesn't exist
            Structured error response sa meaningful message za client
            */
            _logger.LogWarning(ex, "No live AQI data available for {City}", city);
            return NotFound(new { message = $"No air quality data found for city: {city}" });
        }
        catch (Exception ex)
        {
            /*
            === SYSTEM ERROR HANDLING ===
            
            UNEXPECTED ERROR SCENARIOS:
            Network failures, API timeouts, JSON parsing errors
            Infrastructure problems beyond business logic
            
            HTTP 500 INTERNAL SERVER ERROR:
            Indicates system failure, not client error
            Detailed logging za debugging, safe error message za client
            
            ERROR RESPONSE DESIGN:
            Consistent structure sa message i context
            No sensitive information leaked to client
            Sufficient info za client error handling
            */
            _logger.LogError(ex, "Failed to get live data for city {City}", city);
            return StatusCode(500, new
            {
                message = "Failed to retrieve air quality data",
                city
            });
        }
    }
}