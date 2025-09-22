using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SarajevoAir.Application.Interfaces;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CompareController : ControllerBase
{
    private readonly IMeasurementService _measurementService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CompareController> _logger;

    private static readonly string[] ValidCities = {
        "Sarajevo", "Tuzla", "Mostar", "Banja Luka", "Zenica", "Bihac", "Prijedor", "Trebinje"
    };

    public CompareController(
        IMeasurementService measurementService,
        IMemoryCache cache,
        ILogger<CompareController> logger)
    {
        _measurementService = measurementService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Compare air quality data across multiple cities
    /// </summary>
    /// <param name="cities">Comma-separated list of cities (max 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comparison data for multiple cities</returns>
    [HttpGet]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    public async Task<IActionResult> GetCompareData(
        [FromQuery] string cities = "Sarajevo,Tuzla,Mostar",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cityList = cities.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Take(5) // Limit to 5 cities
                .ToArray();

            if (cityList.Length == 0)
            {
                return BadRequest(new { 
                    message = "At least one city must be specified",
                    parameter = "cities"
                });
            }

            // Validate cities
            var invalidCities = cityList.Where(c => !ValidCities.Contains(c, StringComparer.OrdinalIgnoreCase)).ToArray();
            if (invalidCities.Any())
            {
                return BadRequest(new {
                    message = "Invalid cities specified",
                    invalidCities,
                    validCities = ValidCities
                });
            }

            var cacheKey = $"compare-{string.Join("-", cityList.Select(c => c.ToLowerInvariant()).OrderBy(c => c))}";
            
            if (_cache.TryGetValue(cacheKey, out var cachedData))
            {
                _logger.LogDebug("Returning cached compare data for cities: {Cities}", string.Join(", ", cityList));
                return Ok(cachedData);
            }

            var compareData = await _measurementService.GetCompareDataAsync(cityList, cancellationToken);

            // Cache the result for 5 minutes
            _cache.Set(cacheKey, compareData, TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Retrieved compare data for {Count} cities: {Cities}", 
                cityList.Length, string.Join(", ", cityList));
            
            return Ok(compareData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get compare data for cities: {Cities}", cities);
            return StatusCode(500, new { 
                message = "Failed to retrieve comparison data",
                cities
            });
        }
    }

    /// <summary>
    /// Get list of valid cities for comparison
    /// </summary>
    /// <returns>List of available cities</returns>
    [HttpGet("cities")]
    public IActionResult GetValidCities()
    {
        return Ok(new { 
            cities = ValidCities,
            count = ValidCities.Length
        });
    }
}