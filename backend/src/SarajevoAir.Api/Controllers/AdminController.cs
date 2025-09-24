using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAppDbContext dbContext, ILogger<AdminController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get recent AQI records for monitoring
    /// </summary>
    [HttpGet("aqi-records")]
    public async Task<IActionResult> GetAqiRecords(
        [FromQuery] int limit = 20,
        [FromQuery] int hoursBack = 24)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);
            
            var records = await _dbContext.SimpleAqiRecords
                .Where(r => r.Timestamp > cutoffTime)
                .OrderByDescending(r => r.Timestamp)
                .Take(limit)
                .Select(r => new
                {
                    r.Id,
                    r.Timestamp,
                    r.AqiValue,
                    r.City,
                    TimestampLocal = r.Timestamp.ToLocalTime(),
                    Category = GetAqiCategory(r.AqiValue),
                    Color = GetAqiColor(r.AqiValue)
                })
                .ToListAsync();

            var stats = new
            {
                TotalRecords = await _dbContext.SimpleAqiRecords.CountAsync(),
                RecentRecords = records.Count,
                LastUpdate = records.FirstOrDefault()?.Timestamp,
                AverageAqi = records.Any() ? Math.Round(records.Average(r => r.AqiValue), 1) : 0,
                CurrentStatus = records.Any() ? "Active" : "No Recent Data"
            };

            return Ok(new
            {
                Stats = stats,
                Records = records
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AQI records for admin");
            return StatusCode(500, new { message = "Failed to retrieve AQI records" });
        }
    }

    /// <summary>
    /// Get system statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetSystemStats()
    {
        try
        {
            var now = DateTime.UtcNow;
            var last24h = now.AddHours(-24);
            var lastHour = now.AddHours(-1);

            var stats = new
            {
                Database = new
                {
                    TotalRecords = await _dbContext.SimpleAqiRecords.CountAsync(),
                    Last24Hours = await _dbContext.SimpleAqiRecords
                        .CountAsync(r => r.Timestamp > last24h),
                    LastHour = await _dbContext.SimpleAqiRecords
                        .CountAsync(r => r.Timestamp > lastHour)
                },
                LastRecord = await _dbContext.SimpleAqiRecords
                    .OrderByDescending(r => r.Timestamp)
                    .Select(r => new
                    {
                        r.Timestamp,
                        r.AqiValue,
                        MinutesAgo = Math.Round((now - r.Timestamp).TotalMinutes, 1)
                    })
                    .FirstOrDefaultAsync(),
                System = new
                {
                    ServerTime = now,
                    Status = "Running",
                    Version = "1.0.0"
                }
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system stats");
            return StatusCode(500, new { message = "Failed to retrieve system stats" });
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
            <= 50 => "#00ff00",
            <= 100 => "#ffff00",
            <= 150 => "#ff9900",
            <= 200 => "#ff0000",
            <= 300 => "#990099",
            _ => "#7e0023"
        };
    }
}