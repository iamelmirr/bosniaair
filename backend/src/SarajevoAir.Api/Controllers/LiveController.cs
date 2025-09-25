using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/live")]
public class LiveController : ControllerBase
{
    private readonly IAirQualityService _airQualityService;
    private readonly ILogger<LiveController> _logger;

    public LiveController(IAirQualityService airQualityService, ILogger<LiveController> logger)
    {
        _airQualityService = airQualityService;
        _logger = logger;
    }

    /// <summary>
    /// Get current air quality data for a city. Sarajevo responses are cached and persisted, other cities always trigger a fresh fetch.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLiveData(
        [FromQuery] string city = "Sarajevo",
        [FromQuery] bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var forceFresh = refresh || !city.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase);
            var response = await _airQualityService.GetLiveAqiAsync(city, forceFresh, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No live AQI data available for {City}", city);
            return NotFound(new { message = $"No air quality data found for city: {city}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get live data for city {City}", city);
            return StatusCode(500, new
            {
                message = "Failed to retrieve air quality data",
                city
            });
        }
    }
}