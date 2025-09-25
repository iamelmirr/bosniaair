/*using Microsoft.AspNetCore.Mvc;

===========================================================================================using SarajevoAir.Api.Services;

                               LIVE CONTROLLER - GENERAL LIVE AQI ENDPOINTS

===========================================================================================namespace SarajevoAir.Api.Controllers;



PURPOSE: General live AQI endpoint `/api/v1/live?city=...` za svu fronted kompatibilnost/*

Routuje pozive na odgovarajuće specialized servise===========================================================================================

                                    LIVE AQI DATA CONTROLLER  

ROUTING LOGIC:===========================================================================================

- city=Sarajevo → SarajevoService (optimized)

- ostali gradovi → AqicnService (on-demand)PURPOSE & API RESPONSIBILITY:

*/Primary endpoint za real-time air quality data retrieval.

Handles GET requests za current AQI measurements sa caching optimization.

using Microsoft.AspNetCore.Mvc;

using SarajevoAir.Api.Services;REST API DESIGN PATTERNS:

using SarajevoAir.Api.Dtos;1. Resource-Based Routing - /api/v1/live represents live AQI resource

2. Query Parameter Filtering - ?city=Name enables city selection

namespace SarajevoAir.Api.Controllers;3. HTTP Status Code Semantics - 200/404/500 za proper client communication

4. Content Negotiation - JSON responses za web/mobile clients

/// <summary>

/// General live AQI controller za sve gradoveAPI CONTRACT DESIGN:

/// Routuje na odgovarajuće specialized servise┌─────────────────────────┐    ┌──────────────────────────┐    ┌─────────────────────┐

/// </summary>│      HTTP CLIENT        │────│    LIVE CONTROLLER       │────│  AIR QUALITY SERVICE │

[ApiController]│   (Browser/Mobile)      │    │   (This Controller)      │    │   (Business Logic)   │

[Route("api/v1")]└─────────────────────────┘    └──────────────────────────┘    └─────────────────────┘

[Produces("application/json")]         │                              │                              │

public class LiveController : ControllerBase         │ GET /api/v1/live?city=X      │                              │

{         │ ──────────────────────────►  │ GetLiveAqiAsync(city)       │

    private readonly ISarajevoService _sarajevoService;         │                              │ ──────────────────────────► │

    private readonly IAqicnService _aqicnService;         │                              │                              │

    private readonly ILogger<LiveController> _logger;         │ 200 OK + JSON Response       │ ◄────────────────────────── │

         │ ◄────────────────────────── │                              │

    public LiveController(

        ISarajevoService sarajevoService, CACHING STRATEGY INTEGRATION:

        IAqicnService aqicnService, - Sarajevo: Cache-first approach (background refresh maintains freshness)

        ILogger<LiveController> logger)- Other cities: Always fresh (lower usage, prevents stale data)

    {- Client-side: ETags/Cache-Control headers za browser optimization

        _sarajevoService = sarajevoService;

        _aqicnService = aqicnService;PERFORMANCE CHARACTERISTICS:

        _logger = logger;- Cache hit (Sarajevo): ~1-2ms response time

    }- Fresh API call: ~200-500ms response time  

- Concurrent requests: Thread-safe via service layer

    /// <summary>- Error resilience: Graceful degradation sa meaningful error responses

    /// General live AQI endpoint za sve gradove*/

    /// Routuje na optimized services based on city

    /// </summary>/// <summary>

    /// <param name="city">City name (Sarajevo gets special handling)</param>/// REST API controller za live air quality data endpoints

    /// <param name="cancellationToken">Cancellation token</param>/// Optimized za high-frequency requests sa intelligent caching

    /// <returns>Live AQI podatke za specified city</returns>/// </summary>

    [HttpGet("live")][ApiController]

    [ProducesResponseType(typeof(LiveAqiResponse), 200)][Route("api/v1/live")]

    [ProducesResponseType(400)]public class LiveController : ControllerBase

    [ProducesResponseType(500)]{

    public async Task<ActionResult<LiveAqiResponse>> GetLive(    /*

        [FromQuery] string city,    === CONTROLLER DEPENDENCIES ===

        CancellationToken cancellationToken = default)    

    {    DEPENDENCY INJECTION PATTERN:

        if (string.IsNullOrWhiteSpace(city))    IAirQualityService - Core business logic za AQI operations

        {    ILogger - Structured logging za monitoring i debugging

            return BadRequest(new { error = "Bad Request", message = "City parameter is required" });    

        }    CONTROLLER LIFECYCLE:

    Scoped lifetime - New instance per HTTP request

        try    Dependencies resolved od ASP.NET Core DI container

        {    Automatic disposal after request completion

            _logger.LogInformation("Getting live data for city: {City}", city);    */

    private readonly IAirQualityService _airQualityService;

            LiveAqiResponse result;    private readonly ILogger<LiveController> _logger;

            

            if (city.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase))    /// <summary>

            {    /// Constructor za dependency injection setup

                // Use optimized Sarajevo service    /// ASP.NET Core resolves dependencies automatically

                result = await _sarajevoService.GetLiveAsync(false, cancellationToken);    /// </summary>

            }    public LiveController(IAirQualityService airQualityService, ILogger<LiveController> logger)

            else    {

            {        _airQualityService = airQualityService;

                // Use general AQICN service za ostale gradove        _logger = logger;

                result = await _aqicnService.GetCityLiveAsync(city, cancellationToken);    }

            }

    /*

            _logger.LogDebug("Successfully retrieved live data for {City}, AQI: {Aqi}", city, result.OverallAqi);    === PRIMARY API ENDPOINT ===

            return Ok(result);    

        }    HTTP METHOD SELECTION:

        catch (ArgumentException ex)    GET - Safe, idempotent operation za data retrieval

        {    No side effects, cacheable by browsers i proxies

            _logger.LogWarning("Invalid argument for city {City}: {Message}", city, ex.Message);    

            return BadRequest(new { error = "Bad Request", message = ex.Message });    ROUTE TEMPLATE DESIGN:

        }    /api/v1/live - RESTful resource naming

        catch (Exception ex)    Query parameters za filtering i options

        {    

            _logger.LogError(ex, "Error getting live data for city {City}", city);    PARAMETER BINDING STRATEGY:

            return StatusCode(500, new { error = "Internal server error", message = $"Failed to retrieve data for {city}" });    [FromQuery] - Binds URL query parameters to method parameters

        }    Default values enable flexible client usage

    }    CancellationToken supports request cancellation

}    */
    
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