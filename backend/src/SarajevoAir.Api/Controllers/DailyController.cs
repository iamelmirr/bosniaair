using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DailyController : ControllerBase
{
    private readonly IDailyAqiService _dailyAqiService;
    private readonly ILogger<DailyController> _logger;

    public DailyController(IDailyAqiService dailyAqiService, ILogger<DailyController> logger)
    {
        _dailyAqiService = dailyAqiService;
        _logger = logger;
    }

    /// <summary>
    /// Get daily AQI data for the last 7 days based on stored Sarajevo snapshots.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDailyAqi(
        [FromQuery] string city = "Sarajevo",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _dailyAqiService.GetDailyAqiAsync(city, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get daily AQI data for city {City}", city);
            return StatusCode(500, new
            {
                message = "Failed to retrieve daily AQI data",
                city
            });
        }
    }
}