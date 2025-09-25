using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/forecast")]
public class ForecastController : ControllerBase
{
    private readonly IForecastService _forecastService;
    private readonly ILogger<ForecastController> _logger;

    public ForecastController(IForecastService forecastService, ILogger<ForecastController> logger)
    {
        _forecastService = forecastService;
        _logger = logger;
    }

    /// <summary>
    /// Get AQI forecast for the next days. Sarajevo forecasts are cached for two hours, other cities trigger a fresh API call.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetForecastData(
        [FromQuery] string city = "Sarajevo",
        [FromQuery] bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var forceFresh = refresh || !city.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase);
            var response = await _forecastService.GetForecastAsync(city, forceFresh, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get forecast data for city {City}", city);
            return StatusCode(500, new
            {
                message = "Failed to retrieve forecast data",
                city,
                timestamp = DateTime.UtcNow
            });
        }
    }
}