using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DailyController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<DailyController> _logger;

    public DailyController(IMemoryCache cache, ILogger<DailyController> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get daily AQI data for the last 7 days
    /// </summary>
    /// <param name="city">City name (default: Sarajevo)</param>
    /// <returns>Daily AQI data for the last 7 days</returns>
    [HttpGet]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour
    public IActionResult GetDailyAqi([FromQuery] string city = "Sarajevo")
    {
        try
        {
            var cacheKey = $"daily-aqi-{city}";
            
            if (_cache.TryGetValue(cacheKey, out var cachedData))
            {
                return Ok(cachedData);
            }

            // Generate mock data for now
            var dailyData = GenerateMockDailyData(city);

            // Cache for 1 hour
            _cache.Set(cacheKey, dailyData, TimeSpan.FromHours(1));
            
            _logger.LogInformation("Generated daily AQI data for {City}", city);
            
            return Ok(dailyData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get daily AQI data for city {City}", city);
            return StatusCode(500, new { 
                message = "Failed to retrieve daily AQI data",
                city
            });
        }
    }

    private static object GenerateMockDailyData(string city)
    {
        var random = new Random();
        var today = DateTime.UtcNow.Date;
        
        var days = new List<object>();
        
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var aqi = random.Next(25, 150); // Random AQI between 25-150
            var category = GetAqiCategory(aqi);
            
            days.Add(new
            {
                date = date.ToString("yyyy-MM-dd"),
                dayName = date.ToString("dddd"),
                shortDay = date.ToString("ddd"),
                aqi = aqi,
                category = category,
                color = GetAqiColor(aqi)
            });
        }

        return new
        {
            city = city,
            period = "Last 7 days",
            data = days,
            timestamp = DateTime.UtcNow
        };
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
            <= 50 => "#00e400",      // Green
            <= 100 => "#ffff00",     // Yellow
            <= 150 => "#ff7e00",     // Orange
            <= 200 => "#ff0000",     // Red
            <= 300 => "#8f3f97",     // Purple
            _ => "#7e0023"           // Maroon
        };
    }
}