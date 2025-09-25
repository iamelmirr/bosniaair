/*
===========================================================================================
                               CITIES CONTROLLER - OTHER CITIES ON-DEMAND
===========================================================================================

PURPOSE: On-demand AQI data za ostale gradove (ne-Sarajevo)
Jednostavan endpoint bez cache optimizacija

REPLACES: LiveController (za ostale gradove)
DESIGN: Simplifikovano - samo live data, bez forecast/groups za ostale gradove
*/

using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;
using SarajevoAir.Api.Dtos;

namespace SarajevoAir.Api.Controllers;

/// <summary>
/// Controller za on-demand AQI data za ostale gradove (ne-Sarajevo)
/// Jednostavan pristup bez cache optimizacija
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class CitiesController : ControllerBase
{
    private readonly IAqicnService _aqicnService;
    private readonly ISarajevoService _sarajevoService;
    private readonly ILogger<CitiesController> _logger;

    public CitiesController(
        IAqicnService aqicnService, 
        ISarajevoService sarajevoService,
        ILogger<CitiesController> logger)
    {
        _aqicnService = aqicnService;
        _sarajevoService = sarajevoService;
        _logger = logger;
    }

    /// <summary>
    /// General live endpoint - query parameter format /live?city=...
    /// Za Sarajevo će koristiti već učitane podatke, za ostale gradove on-demand
    /// </summary>
    /// <param name="city">Ime grada iz query parametra</param>
    /// <returns>Live AQI podaci za specified grad</returns>
    [HttpGet("live")]
    [ProducesResponseType(typeof(LiveAqiResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)] 
    [ProducesResponseType(500)]
    public async Task<ActionResult<LiveAqiResponse>> GetLive(
        [FromQuery] string city,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            _logger.LogWarning("City parameter is missing in query");
            return BadRequest(new { error = "Bad Request", message = "City parameter is required" });
        }

        try
        {
            _logger.LogInformation("Getting live data for city: {City}", city);
            
            LiveAqiResponse result;
            
            // Za Sarajevo koristi optimized service koji koristi već učitane podatke
            if (city.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Using optimized Sarajevo service for already loaded data");
                result = await _sarajevoService.GetLiveAsync(false, cancellationToken);
            }
            else
            {
                // Za ostale gradove koristi on-demand AQICN service
                result = await _aqicnService.GetCityLiveAsync(city, cancellationToken);
            }
            
            _logger.LogDebug("Successfully retrieved live data for {City}, AQI: {Aqi}", city, result.OverallAqi);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument for city {City}: {Message}", city, ex.Message);
            return BadRequest(new { error = "Bad Request", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No data available"))
        {
            _logger.LogWarning("No data available for city: {City}", city);
            return NotFound(new { error = "Not Found", message = $"No air quality data available for city: {city}" });
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            _logger.LogWarning("City not found: {City}", city);
            return NotFound(new { error = "Not Found", message = $"City not found: {city}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting live data for city {City}", city);
            return StatusCode(500, new { error = "Internal server error", message = $"Failed to retrieve data for city: {city}" });
        }
    }

    /// <summary>
    /// Dobija live AQI podatke za bilo koji grad osim Sarajevo
    /// Za Sarajevo koristiti SarajevoController endpoints (/api/v1/sarajevo/*)
    /// </summary>
    /// <param name="city">Ime grada</param>
    /// <returns>Live AQI podaci za specified grad</returns>
    [HttpGet("cities/{city}/live")]
    [ProducesResponseType(typeof(LiveAqiResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)] 
    [ProducesResponseType(500)]
    public async Task<ActionResult<LiveAqiResponse>> GetCityLive(
        string city,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            _logger.LogWarning("City name is missing in request");
            return BadRequest(new { error = "Bad Request", message = "City name is required" });
        }

        // Redirect Sarajevo requests to specialized controller
        if (city.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Redirecting Sarajevo request to specialized controller");
            return BadRequest(new { 
                error = "Bad Request", 
                message = "Use /api/v1/sarajevo/live for Sarajevo data",
                redirectUrl = "/api/v1/sarajevo/live"
            });
        }

        try
        {
            _logger.LogInformation("Getting live data for city: {City}", city);
            
            var result = await _aqicnService.GetCityLiveAsync(city, cancellationToken);
            
            _logger.LogDebug("Successfully retrieved live data for {City}, AQI: {Aqi}", city, result.OverallAqi);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument for city {City}: {Message}", city, ex.Message);
            return BadRequest(new { error = "Bad Request", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No data available"))
        {
            _logger.LogWarning("No data available for city: {City}", city);
            return NotFound(new { error = "Not Found", message = $"No air quality data available for city: {city}" });
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            _logger.LogWarning("City not found: {City}", city);
            return NotFound(new { error = "Not Found", message = $"City not found: {city}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting live data for city {City}", city);
            return StatusCode(500, new { error = "Internal server error", message = $"Failed to retrieve data for city: {city}" });
        }
    }
}