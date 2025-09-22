using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SarajevoAir.Application.Interfaces;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LiveController : ControllerBase
{
    private readonly IMeasurementService _measurementService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LiveController> _logger;

    public LiveController(
        IMeasurementService measurementService,
        IMemoryCache cache,
        ILogger<LiveController> logger)
    {
        _measurementService = measurementService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get current air quality data for a city
    /// </summary>
    /// <param name="city">City name (default: Sarajevo)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current air quality information</returns>
    [HttpGet]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    public async Task<IActionResult> GetLiveData(
        [FromQuery] string city = "Sarajevo", 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"live-{city.ToLowerInvariant()}";
            
            if (_cache.TryGetValue(cacheKey, out var cachedData))
            {
                _logger.LogDebug("Returning cached live data for {City}", city);
                return Ok(cachedData);
            }

            var liveData = await _measurementService.GetLiveDataAsync(city, cancellationToken);
            
            if (liveData == null)
            {
                return NotFound(new { 
                    message = $"No air quality data found for {city}",
                    city 
                });
            }

            // Cache the result for 5 minutes
            _cache.Set(cacheKey, liveData, TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Retrieved live air quality data for {City}: AQI {Aqi}", city, liveData.Aqi);
            
            return Ok(liveData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get live data for city {City}", city);
            return StatusCode(500, new { 
                message = "Failed to retrieve air quality data", 
                city 
            });
        }
    }
}