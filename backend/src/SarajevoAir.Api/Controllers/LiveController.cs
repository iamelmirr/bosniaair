using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LiveController : ControllerBase
{
    private readonly IAqicnClient _aqicnClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LiveController> _logger;

    public LiveController(
        IAqicnClient aqicnClient,
        IMemoryCache cache,
        ILogger<LiveController> logger)
    {
        _aqicnClient = aqicnClient;
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

            var aqicnResponse = await _aqicnClient.GetCityDataAsync(city.ToLowerInvariant(), cancellationToken);
            
            if (aqicnResponse?.Data == null)
            {
                return NotFound(new { message = $"No air quality data found for city: {city}" });
            }

            // Convert AQICN response to frontend-compatible format
            var responseData = new
            {
                city = aqicnResponse.Data.City?.Name ?? city,
                overallAqi = aqicnResponse.Data.Aqi,
                aqiCategory = GetAqiCategory(aqicnResponse.Data.Aqi),
                color = GetAqiColor(aqicnResponse.Data.Aqi),
                healthMessage = GetHealthMessage(aqicnResponse.Data.Aqi),
                timestamp = DateTime.UtcNow,
                measurements = GetMeasurements(aqicnResponse.Data, city),
                dominantPollutant = aqicnResponse.Data.DominentPol ?? "pm25"
            };

            // Cache the result for 5 minutes
            _cache.Set(cacheKey, responseData, TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Retrieved live air quality data for {City}: AQI {Aqi}", city, aqicnResponse.Data.Aqi);
            
            return Ok(responseData);
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
            <= 50 => "#00e400",     // Green
            <= 100 => "#ffff00",    // Yellow
            <= 150 => "#ff7e00",    // Orange
            <= 200 => "#ff0000",    // Red
            <= 300 => "#8f3f97",    // Purple
            _ => "#7e0023"          // Maroon
        };
    }

    private static string GetHealthMessage(int aqi)
    {
        return aqi switch
        {
            <= 50 => "Air quality is considered satisfactory, and air pollution poses little or no risk.",
            <= 100 => "Air quality is acceptable; however, there may be some health concern for a very small number of people who are unusually sensitive to air pollution.",
            <= 150 => "Members of sensitive groups may experience health effects. The general public is not likely to be affected.",
            <= 200 => "Everyone may begin to experience health effects; members of sensitive groups may experience more serious health effects.",
            <= 300 => "Health warnings of emergency conditions. The entire population is more likely to be affected.",
            _ => "Health alert: everyone may experience more serious health effects."
        };
    }

    private static object[] GetMeasurements(SarajevoAir.Api.Models.AqicnData data, string city)
    {
        var measurements = new List<object>();
        
        // Get coordinates from city data (default to Sarajevo if not available)
        var latitude = data.City?.Geo?.Length > 0 ? data.City.Geo[0] : 43.8563;
        var longitude = data.City?.Geo?.Length > 1 ? data.City.Geo[1] : 18.4131;

        if (data.Iaqi?.Pm25 != null)
        {
            measurements.Add(new
            {
                id = Guid.NewGuid().ToString(),
                city,
                locationName = data.City?.Name ?? "City Center",
                parameter = "pm25",
                value = data.Iaqi.Pm25.V,
                unit = "µg/m³",
                timestamp = DateTime.UtcNow,
                sourceName = "AQICN",
                coordinates = new { latitude, longitude }
            });
        }

        if (data.Iaqi?.Pm10 != null)
        {
            measurements.Add(new
            {
                id = Guid.NewGuid().ToString(),
                city,
                locationName = data.City?.Name ?? "City Center",
                parameter = "pm10",
                value = data.Iaqi.Pm10.V,
                unit = "µg/m³",
                timestamp = DateTime.UtcNow,
                sourceName = "AQICN",
                coordinates = new { latitude, longitude }
            });
        }

        if (data.Iaqi?.O3 != null)
        {
            measurements.Add(new
            {
                id = Guid.NewGuid().ToString(),
                city,
                locationName = data.City?.Name ?? "City Center",
                parameter = "o3",
                value = data.Iaqi.O3.V,
                unit = "µg/m³",
                timestamp = DateTime.UtcNow,
                sourceName = "AQICN",
                coordinates = new { latitude, longitude }
            });
        }

        if (data.Iaqi?.No2 != null)
        {
            measurements.Add(new
            {
                id = Guid.NewGuid().ToString(),
                city,
                locationName = data.City?.Name ?? "City Center",
                parameter = "no2",
                value = data.Iaqi.No2.V,
                unit = "µg/m³",
                timestamp = DateTime.UtcNow,
                sourceName = "AQICN",
                coordinates = new { latitude, longitude }
            });
        }

        if (data.Iaqi?.So2 != null)
        {
            measurements.Add(new
            {
                id = Guid.NewGuid().ToString(),
                city,
                locationName = data.City?.Name ?? "City Center",
                parameter = "so2",
                value = data.Iaqi.So2.V,
                unit = "µg/m³",
                timestamp = DateTime.UtcNow,
                sourceName = "AQICN",
                coordinates = new { latitude, longitude }
            });
        }

        if (data.Iaqi?.Co != null)
        {
            measurements.Add(new
            {
                id = Guid.NewGuid().ToString(),
                city,
                locationName = data.City?.Name ?? "City Center",
                parameter = "co",
                value = data.Iaqi.Co.V,
                unit = "mg/m³",
                timestamp = DateTime.UtcNow,
                sourceName = "AQICN",
                coordinates = new { latitude, longitude }
            });
        }

        return measurements.ToArray();
    }
}