using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

/*
===========================================================================================
                                 AIR QUALITY FORECAST CONTROLLER
===========================================================================================

PURPOSE & API RESPONSIBILITY:
Dedicated endpoint za air quality prediction data (24-hour forecasts).
Handles long-term caching strategy due to lower update frequency od forecast data.

FORECAST vs LIVE DATA DIFFERENCES:
┌────────────────────┬─────────────────────┬─────────────────────┐
│      ASPECT        │    LIVE CONTROLLER  │ FORECAST CONTROLLER │
├────────────────────┼─────────────────────┼─────────────────────┤
│ Update Frequency   │ Every 10 minutes    │ Every 2 hours       │
│ Cache Duration     │ 10 minutes          │ 120 minutes         │
│ Data Volatility    │ High (real-time)    │ Low (predictive)    │
│ API Cost           │ Higher              │ Lower               │
│ User Usage Pattern │ Frequent checks     │ Planning ahead      │
└────────────────────┴─────────────────────┴─────────────────────┘

REST API DESIGN:
- Resource: /api/v1/forecast - Represents forecast predictions
- Same parameter pattern kao live endpoint za consistency
- Extended cache TTL appropriate za forecast data nature
- Error handling optimized za longer response times

BUSINESS VALUE:
- Enables air quality planning i outdoor activity decisions
- Supports health advisory systems za sensitive individuals
- Provides predictive analytics za city environmental planning
*/

/// <summary>
/// REST API controller za air quality forecast endpoints
/// Optimized za longer caching due to predictive data nature
/// </summary>
[ApiController]
[Route("api/v1/forecast")]
public class ForecastController : ControllerBase
{
    /*
    === FORECAST-SPECIFIC DEPENDENCIES ===
    
    SPECIALIZED SERVICE INJECTION:
    IForecastService - Business logic za prediction data handling
    Separate od live service enables different caching strategies
    */
    private readonly IForecastService _forecastService;
    private readonly ILogger<ForecastController> _logger;

    /// <summary>
    /// Constructor sa forecast-specific service dependencies
    /// </summary>
    public ForecastController(IForecastService forecastService, ILogger<ForecastController> logger)
    {
        _forecastService = forecastService;
        _logger = logger;
    }

    /*
    === FORECAST API ENDPOINT ===
    
    EXTENDED CACHING STRATEGY:
    Forecast data changes less frequently than live measurements
    2-hour cache duration vs 10-minute za live data
    Balances freshness sa reduced API usage costs
    
    PREDICTION DATA CHARACTERISTICS:
    - 24-hour AQI predictions
    - Hourly breakdowns za detailed planning
    - Weather correlation data
    - Confidence intervals za predictions
    */
    
    /// <summary>
    /// Retrieves air quality forecast predictions za specified city
    /// Returns 24-hour hourly predictions sa confidence metrics
    /// 
    /// Example requests:
    /// GET /api/v1/forecast - Sarajevo forecast (cached 2 hours)
    /// GET /api/v1/forecast?city=Banja Luka - Fresh forecast za other city
    /// GET /api/v1/forecast?refresh=true - Force fresh forecast data
    /// </summary>
    /// <param name="city">Target city za forecast prediction</param>
    /// <param name="refresh">Bypass cache i get fresh predictions</param>
    /// <param name="cancellationToken">Request cancellation support</param>
    /// <returns>24-hour AQI forecast sa hourly predictions</returns>
    [HttpGet]
    public async Task<IActionResult> GetForecastData(
        [FromQuery] string city = "Sarajevo",
        [FromQuery] bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            /*
            === CACHING LOGIC ===
            
            SAME PATTERN kao LiveController ali sa different service
            Service layer handles extended TTL za forecast caching
            */
            var forceFresh = refresh || !city.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase);
            var response = await _forecastService.GetForecastAsync(city, forceFresh, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            /*
            === FORECAST ERROR HANDLING ===
            
            SIMPLIFIED ERROR STRATEGY:
            Forecast failures less critical than live data
            Single catch-all approach sa detailed logging
            Timestamp added za temporal debugging context
            
            REASONING:
            Forecast predictions inherently uncertain
            Network failures more acceptable za planning data
            Users can fall back to live data za immediate decisions
            */
            _logger.LogError(ex, "Failed to get forecast data for city {City}", city);
            return StatusCode(500, new
            {
                message = "Failed to retrieve forecast data",
                city,
                timestamp = DateTime.UtcNow  // Temporal context za debugging
            });
        }
    }
}