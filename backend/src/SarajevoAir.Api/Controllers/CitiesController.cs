using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CitiesController : ControllerBase
{
    private readonly IAqicnClient _aqicnClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CitiesController> _logger;

    private static readonly string[] ValidCities = {
        "Sarajevo", "Tuzla", "Mostar", "Banja Luka", "Zenica", "Bihac", "Prijedor", "Trebinje"
    };

    public CitiesController(
        IAqicnClient aqicnClient,
        IMemoryCache cache,
        ILogger<CitiesController> logger)
    {
        _aqicnClient = aqicnClient;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get list of valid cities for selection
    /// </summary>
    /// <returns>List of available cities</returns>
    [HttpGet]
    public IActionResult GetValidCities()
    {
        return Ok(new { 
            cities = ValidCities.Select(city => new {
                name = city,
                displayName = city
            }),
            count = ValidCities.Length
        });
    }

    /// <summary>
    /// Get current AQI data for a specific city
    /// </summary>
    /// <param name="city">City name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current AQI data for the city</returns>
    [HttpGet("{city}")]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    public async Task<IActionResult> GetCityAqi(
        string city,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return BadRequest(new { message = "City name is required" });
            }

            // Validate city
            if (!ValidCities.Contains(city, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new {
                    message = "Invalid city specified",
                    city,
                    validCities = ValidCities
                });
            }

            var cacheKey = $"city-aqi-{city.ToLowerInvariant()}";
            
            if (_cache.TryGetValue(cacheKey, out var cachedData))
            {
                _logger.LogDebug("Returning cached AQI data for city: {City}", city);
                return Ok(cachedData);
            }

            var aqiData = await _aqicnClient.GetCityDataAsync(city.ToLowerInvariant(), cancellationToken);

            if (aqiData?.Data == null)
            {
                return NotFound(new { 
                    message = "AQI data not found for city",
                    city
                });
            }

            var result = new {
                city = city,
                aqi = aqiData.Data.Aqi,
                category = GetAqiCategory(aqiData.Data.Aqi),
                color = GetAqiColor(aqiData.Data.Aqi),
                timestamp = DateTime.UtcNow,
                dominantPollutant = aqiData.Data.DominentPol
            };

            // Cache the result for 5 minutes
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Retrieved AQI data for city: {City}, AQI: {Aqi}", city, aqiData.Data.Aqi);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AQI data for city: {City}", city);
            return StatusCode(500, new { 
                message = "Failed to retrieve AQI data",
                city
            });
        }
    }

    private static string GetAqiCategory(int aqi)
    {
        return aqi switch
        {
            <= 50 => "Good",
            <= 100 => "Moderate", 
            <= 150 => "Unhealthy for Sensitive Groups",
            <= 200 => "Unhealthy",
            <= 300 => "Very Unhealthy",
            _ => "Hazardous"
        };
    }

    private static string GetAqiColor(int aqi)
    {
        return aqi switch
        {
            <= 50 => "#4ade80",    // Green
            <= 100 => "#ffff00",   // Yellow  
            <= 150 => "#ff7e00",   // Orange
            <= 200 => "#ef4444",   // Red
            <= 300 => "#a855f7",   // Purple
            _ => "#7c2d12"         // Maroon
        };
    }
}