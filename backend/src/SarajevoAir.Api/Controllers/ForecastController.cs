using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ForecastController : ControllerBase
{
    private readonly IAqicnClient _aqicnClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ForecastController> _logger;

    public ForecastController(
        IAqicnClient aqicnClient, 
        IMemoryCache cache, 
        ILogger<ForecastController> logger)
    {
        _aqicnClient = aqicnClient;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get air quality forecast for a city
    /// </summary>
    /// <param name="city">City name (default: Sarajevo)</param>
    /// <returns>Air quality forecast data</returns>
    [HttpGet]
    [ResponseCache(Duration = 7200)] // Cache for 2 hours
    public async Task<IActionResult> GetForecastData(
        [FromQuery] string city = "Sarajevo",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"forecast-{city.ToLowerInvariant()}";
            
            if (_cache.TryGetValue(cacheKey, out var cachedData))
            {
                _logger.LogDebug("Returning cached forecast data for {City}", city);
                return Ok(cachedData);
            }

            var aqicnData = await _aqicnClient.GetCityDataAsync(city, cancellationToken);
            
            if (aqicnData?.Data?.Forecast?.Daily == null)
            {
                _logger.LogWarning("No forecast data available for city {City}", city);
                return Ok(new { 
                    city = city,
                    forecast = new object[0],
                    message = "No forecast data available",
                    timestamp = DateTime.UtcNow
                });
            }

            var forecastData = ProcessForecastData(aqicnData.Data.Forecast.Daily, city);

            // Cache for 2 hours
            _cache.Set(cacheKey, forecastData, TimeSpan.FromHours(2));
            
            _logger.LogInformation("Retrieved forecast data for {City}", city);
            
            return Ok(forecastData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get forecast data for city {City}", city);
            return StatusCode(500, new { 
                message = "Failed to retrieve forecast data",
                city,
                timestamp = DateTime.UtcNow
            });
        }
    }

    private static object ProcessForecastData(Models.AqicnDailyForecast dailyForecast, string city)
    {
        var forecastDays = new List<object>();
        
        // Process PM2.5 forecast (main pollutant)
        if (dailyForecast.Pm25 != null)
        {
            foreach (var day in dailyForecast.Pm25.Take(7)) // Max 7 days
            {
                var pm10Day = dailyForecast.Pm10?.FirstOrDefault(d => d.Day == day.Day);
                var o3Day = dailyForecast.O3?.FirstOrDefault(d => d.Day == day.Day);
                
                // Calculate AQI from PM2.5
                var aqi = CalculateAqiFromPm25(day.Avg);
                
                forecastDays.Add(new
                {
                    date = day.Day,
                    aqi = aqi,
                    category = GetAqiCategory(aqi),
                    color = GetAqiColor(aqi),
                    pollutants = new
                    {
                        pm25 = new { avg = day.Avg, min = day.Min, max = day.Max },
                        pm10 = pm10Day != null ? new { avg = pm10Day.Avg, min = pm10Day.Min, max = pm10Day.Max } : null,
                        o3 = o3Day != null ? new { avg = o3Day.Avg, min = o3Day.Min, max = o3Day.Max } : null
                    }
                });
            }
        }
        
        return new
        {
            city = city,
            forecast = forecastDays,
            timestamp = DateTime.UtcNow
        };
    }

    private static int CalculateAqiFromPm25(double pm25)
    {
        // EPA AQI calculation for PM2.5
        if (pm25 <= 12.0) return (int)Math.Round((50.0 / 12.0) * pm25);
        if (pm25 <= 35.4) return (int)Math.Round(((100 - 51) / (35.4 - 12.1)) * (pm25 - 12.1) + 51);
        if (pm25 <= 55.4) return (int)Math.Round(((150 - 101) / (55.4 - 35.5)) * (pm25 - 35.5) + 101);
        if (pm25 <= 150.4) return (int)Math.Round(((200 - 151) / (150.4 - 55.5)) * (pm25 - 55.5) + 151);
        if (pm25 <= 250.4) return (int)Math.Round(((300 - 201) / (250.4 - 150.5)) * (pm25 - 150.5) + 201);
        return (int)Math.Round(((500 - 301) / (500.4 - 250.5)) * (pm25 - 250.5) + 301);
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
            <= 50 => "#22C55E",     // Green
            <= 100 => "#EAB308",    // Yellow
            <= 150 => "#F97316",    // Orange
            <= 200 => "#EF4444",    // Red
            <= 300 => "#A855F7",    // Purple
            _ => "#7C2D12"          // Brown
        };
    }
}