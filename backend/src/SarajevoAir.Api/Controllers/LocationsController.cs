using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SarajevoAir.Application.Interfaces;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly IMeasurementService _measurementService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        IMeasurementService measurementService,
        IMemoryCache cache,
        ILogger<LocationsController> logger)
    {
        _measurementService = measurementService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get sensor locations for a city (for map display)
    /// </summary>
    /// <param name="city">City name (default: Sarajevo)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of sensor locations with coordinates</returns>
    [HttpGet]
    [ResponseCache(Duration = 1800)] // Cache for 30 minutes
    public async Task<IActionResult> GetLocations(
        [FromQuery] string city = "Sarajevo",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"locations-{city.ToLowerInvariant()}";
            
            if (_cache.TryGetValue(cacheKey, out var cachedData))
            {
                _logger.LogDebug("Returning cached locations for {City}", city);
                return Ok(cachedData);
            }

            var locations = await _measurementService.GetLocationsAsync(city, cancellationToken);

            var response = new
            {
                city,
                count = locations.Count,
                locations = locations.Select(l => new
                {
                    id = l.Id,
                    name = l.Name,
                    latitude = l.Latitude,
                    longitude = l.Longitude,
                    lastMeasurement = l.LastMeasurement,
                    hasCoordinates = l.Latitude.HasValue && l.Longitude.HasValue
                })
            };

            // Cache the result for 30 minutes (locations don't change often)
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(30));
            
            _logger.LogInformation("Retrieved {Count} locations for {City}", locations.Count, city);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get locations for city {City}", city);
            return StatusCode(500, new { 
                message = "Failed to retrieve location data",
                city
            });
        }
    }
}