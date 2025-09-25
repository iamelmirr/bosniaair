/*
===========================================================================================
                               SARAJEVO CONTROLLER - SPECIALIZED ENDPOINTS
===========================================================================================

PURPOSE: Optimizovani endpoints za sve Sarajevo funkcionalnosti
- /live - samo live AQI
- /forecast - samo forecast  
- /complete - live + forecast u jednom pozivu (optimizacija za glavnu stranicu)

REPLACES: LiveController (za Sarajevo), ForecastController, GroupsController
PERFORMANCE: Kombinovani /complete endpoint smanjuje HTTP pozive za frontend
*/

using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;
using SarajevoAir.Api.Dtos;

namespace SarajevoAir.Api.Controllers;

/// <summary>
/// Specialized controller za sve Sarajevo air quality operacije
/// Optimizovan za glavnu funkcionalnost aplikacije
/// </summary>
[ApiController]
[Route("api/v1/sarajevo")]
[Produces("application/json")]
public class SarajevoController : ControllerBase
{
    private readonly ISarajevoService _sarajevoService;
    private readonly ILogger<SarajevoController> _logger;

    public SarajevoController(ISarajevoService sarajevoService, ILogger<SarajevoController> logger)
    {
        _sarajevoService = sarajevoService;
        _logger = logger;
    }

    /// <summary>
    /// Dobija live AQI podatke za Sarajevo
    /// </summary>
    /// <param name="forceFresh">Forsiraj fresh API poziv (zaobiđi cache)</param>
    /// <returns>Trenutni AQI podaci za Sarajevo</returns>
    [HttpGet("live")]
    [ProducesResponseType(typeof(LiveAqiResponse), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<LiveAqiResponse>> GetLive(
        [FromQuery] bool forceFresh = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting live data for Sarajevo, forceFresh: {ForceFresh}", forceFresh);
            
            var result = await _sarajevoService.GetLiveAsync(forceFresh, cancellationToken);
            
            _logger.LogDebug("Successfully retrieved live data for Sarajevo, AQI: {Aqi}", result.OverallAqi);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Sarajevo live data");
            return StatusCode(500, new { error = "Internal server error", message = "Failed to retrieve live data for Sarajevo" });
        }
    }

    /// <summary>
    /// Dobija forecast podatke za Sarajevo
    /// </summary>
    /// <param name="forceFresh">Forsiraj fresh API poziv (zaobiđi cache)</param>
    /// <returns>Forecast podaci za Sarajevo</returns>
    [HttpGet("forecast")]
    [ProducesResponseType(typeof(ForecastResponse), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ForecastResponse>> GetForecast(
        [FromQuery] bool forceFresh = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting forecast data for Sarajevo, forceFresh: {ForceFresh}", forceFresh);
            
            var result = await _sarajevoService.GetForecastAsync(forceFresh, cancellationToken);
            
            _logger.LogDebug("Successfully retrieved forecast data for Sarajevo, days: {DayCount}", result.Forecast?.Count ?? 0);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Sarajevo forecast data");
            return StatusCode(500, new { error = "Internal server error", message = "Failed to retrieve forecast data for Sarajevo" });
        }
    }

    /// <summary>
    /// Kombinovani endpoint - live + forecast za Sarajevo u jednom pozivu
    /// OPTIMIZACIJA: Glavna stranica koristi ovaj endpoint da smanji broj HTTP poziva
    /// </summary>
    /// <param name="forceFresh">Forsiraj fresh API poziv za oba dataseta</param>
    /// <returns>Kombinovani live i forecast podaci</returns>
    [HttpGet("complete")]
    [ProducesResponseType(typeof(SarajevoCompleteDto), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SarajevoCompleteDto>> GetComplete(
        [FromQuery] bool forceFresh = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting complete data for Sarajevo, forceFresh: {ForceFresh}", forceFresh);
            
            var result = await _sarajevoService.GetCompleteAsync(forceFresh, cancellationToken);
            
            _logger.LogDebug("Successfully retrieved complete data for Sarajevo, AQI: {Aqi}, Forecast days: {DayCount}", 
                result.LiveData.OverallAqi, 
                result.ForecastData.Forecast?.Count ?? 0);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting complete Sarajevo data");
            return StatusCode(500, new { error = "Internal server error", message = "Failed to retrieve complete data for Sarajevo" });
        }
    }
}