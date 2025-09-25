using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

/*
===========================================================================================
                                CITY COMPARISON CONTROLLER
===========================================================================================

PURPOSE & COMPARATIVE ANALYSIS:
Multi-city AQI comparison endpoint za relative air quality assessment.
Enables users to compare multiple cities simultaneously za travel/relocation decisions.

COMPARISON USE CASES:
1. Travel Planning - "Which city has better air quality today?"
2. Relocation Decisions - "How does Sarajevo compare to other BiH cities?"  
3. Regional Analysis - "Which areas have cleanest air right now?"
4. Health-Based Choices - "Where should sensitive individuals avoid?"

MULTI-CITY API PATTERN:
┌─────────────────────┐    ┌──────────────────────────┐    ┌─────────────────────┐
│     CLIENT          │────│   COMPARE CONTROLLER     │────│  COMPARISON SERVICE  │
│ (Planning Decision) │    │  (Multi-City Endpoint)   │    │ (Parallel API Calls) │
└─────────────────────┘    └──────────────────────────┘    └─────────────────────┘
                                      │                              │
                                      │                              ▼
                           ┌─────────────────────┐      ┌─────────────────────┐
                           │ Structured Response │      │   WAQI API Calls   │
                           │ - City Rankings     │      │ - Sarajevo Station  │
                           │ - AQI Comparisons   │      │ - Tuzla Station     │
                           │ - Health Advice     │      │ - Zenica Station    │
                           └─────────────────────┘      └─────────────────────┘

PERFORMANCE CONSIDERATIONS:
- Always fresh data (no caching za fairness)
- Parallel API calls za faster response
- Request timeout za slow external APIs
- Graceful degradation za partial failures

DATA FRESHNESS PRIORITY:
Comparison requires synchronous data collection za accurate results.
Cached data could skew comparisons if cities have different cache states.
*/

/// <summary>
/// REST API controller za multi-city air quality comparisons
/// Provides side-by-side AQI analysis za decision support
/// </summary>
[ApiController]
[Route("api/v1/compare")]
public class CompareController : ControllerBase
{
    /*
    === COMPARISON-SPECIFIC DEPENDENCIES ===
    
    SPECIALIZED SERVICE:
    ICityComparisonService - Multi-city data collection i ranking logic
    Handles parallel API calls, result aggregation, i comparative analysis
    */
    private readonly ICityComparisonService _comparisonService;
    private readonly ILogger<CompareController> _logger;

    /// <summary>
    /// Constructor za comparison service injection
    /// </summary>
    public CompareController(ICityComparisonService comparisonService, ILogger<CompareController> logger)
    {
        _comparisonService = comparisonService;
        _logger = logger;
    }

    /*
    === MULTI-CITY COMPARISON ENDPOINT ===
    
    ALWAYS FRESH DATA STRATEGY:
    No caching za comparison operations ensures fair results
    All cities collected simultaneously za temporal consistency
    Eliminates cache skew gdje different cities have different data ages
    
    INPUT PARAMETER FORMAT:
    cities="Sarajevo,Tuzla,Zenica" - Comma-separated city list
    Single city comparison against default metrics
    Flexible input handling u service layer
    */
    
    /// <summary>
    /// Compares current air quality across multiple cities
    /// Performs fresh API calls za all cities to ensure fair comparison
    /// 
    /// Example requests:
    /// GET /api/v1/compare - Sarajevo comparison (baseline)
    /// GET /api/v1/compare?cities=Sarajevo,Tuzla,Zenica - Multi-city ranking
    /// GET /api/v1/compare?cities=Banja%20Luka,Mostar - Two-city comparison
    /// 
    /// Response includes:
    /// - City rankings by AQI level
    /// - Side-by-side AQI comparisons  
    /// - Best/worst air quality identification
    /// - Synchronized measurement timestamps
    /// </summary>
    /// <param name="cities">Comma-separated list od cities to compare</param>
    /// <param name="cancellationToken">Parallel API call cancellation</param>
    /// <returns>Comparative analysis sa city rankings</returns>
    [HttpGet]
    public async Task<IActionResult> CompareCities(
        [FromQuery] string cities = "Sarajevo",
        CancellationToken cancellationToken = default)
    {
        try
        {
            /*
            === PARALLEL COMPARISON EXECUTION ===
            
            SERVICE LAYER HANDLES:
            - Comma-separated string parsing
            - Parallel API calls za all cities  
            - Result aggregation i ranking
            - Error handling za partial failures
            - Response structure normalization
            */
            var response = await _comparisonService.CompareCitiesAsync(cities, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            /*
            === COMPARISON ERROR HANDLING ===
            
            COMPLEX FAILURE SCENARIOS:
            Multiple simultaneous API calls increase failure probability
            Partial failures (some cities succeed, others fail)
            Network timeout za slow responses
            Invalid city name handling
            
            DETAILED ERROR RESPONSE:
            Include original exception message za debugging
            Helps identify which specific city/API caused failure
            */
            _logger.LogError(ex, "Failed to compare cities {Cities}", cities);
            return StatusCode(500, new
            {
                error = "Failed to compare cities",
                details = ex.Message  // More detailed error info za multi-city scenarios
            });
        }
    }
}
