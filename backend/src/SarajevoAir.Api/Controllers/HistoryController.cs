using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SarajevoAir.Application.Interfaces;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HistoryController : ControllerBase
{
    private readonly IMeasurementService _measurementService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(
        IMeasurementService measurementService,
        IMemoryCache cache,
        ILogger<HistoryController> logger)
    {
        _measurementService = measurementService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get historical air quality data for a city
    /// </summary>
    /// <param name="city">City name (default: Sarajevo)</param>
    /// <param name="days">Number of days to retrieve (1-30, default: 7)</param>
    /// <param name="resolution">Data resolution: 'hour' or 'day' (default: day)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical air quality data</returns>
    [HttpGet]
    [ResponseCache(Duration = 600)] // Cache for 10 minutes
    public async Task<IActionResult> GetHistoryData(
        [FromQuery] string city = "Sarajevo",
        [FromQuery] int days = 7,
        [FromQuery] string resolution = "day",
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate parameters
            if (days < 1 || days > 30)
            {
                return BadRequest(new { 
                    message = "Days parameter must be between 1 and 30",
                    parameter = "days",
                    value = days
                });
            }

            if (!resolution.Equals("hour", StringComparison.OrdinalIgnoreCase) &&
                !resolution.Equals("day", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new {
                    message = "Resolution must be 'hour' or 'day'",
                    parameter = "resolution",
                    value = resolution
                });
            }

            var cacheKey = $"history-{city.ToLowerInvariant()}-{days}-{resolution.ToLowerInvariant()}";
            
            if (_cache.TryGetValue(cacheKey, out var cachedData))
            {
                _logger.LogDebug("Returning cached history data for {City}", city);
                return Ok(cachedData);
            }

            var historyData = await _measurementService.GetHistoryDataAsync(
                city, days, resolution, cancellationToken);

            // Cache the result for 10 minutes
            _cache.Set(cacheKey, historyData, TimeSpan.FromMinutes(10));
            
            _logger.LogInformation("Retrieved {Resolution} history data for {City}: {Days} days, {DataPoints} points", 
                resolution, city, days, historyData.Data.Count);
            
            return Ok(historyData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get history data for city {City}", city);
            return StatusCode(500, new { 
                message = "Failed to retrieve historical data",
                city,
                days,
                resolution
            });
        }
    }
}