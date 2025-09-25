using System.Linq;
using Microsoft.Extensions.Logging;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Repositories;

namespace SarajevoAir.Api.Services;

public interface IDailyAqiService
{
    Task<DailyAqiResponse> GetDailyAqiAsync(string city, CancellationToken cancellationToken = default);
}

public class DailyAqiService : IDailyAqiService
{
    private readonly IAqiRepository _repository;
    private readonly IAirQualityService _airQualityService;
    private readonly ILogger<DailyAqiService> _logger;

    public DailyAqiService(
        IAqiRepository repository,
        IAirQualityService airQualityService,
        ILogger<DailyAqiService> logger)
    {
        _repository = repository;
        _airQualityService = airQualityService;
        _logger = logger;
    }

    public async Task<DailyAqiResponse> GetDailyAqiAsync(string city, CancellationToken cancellationToken = default)
    {
        var targetCity = string.IsNullOrWhiteSpace(city) ? "Sarajevo" : city.Trim();
        var today = DateTime.UtcNow.Date;
        var startDate = today.AddDays(-6);

        var records = await _repository.GetRangeAsync(targetCity, startDate, cancellationToken);
        var grouped = records.GroupBy(r => r.Timestamp.Date).ToDictionary(g => g.Key, g => g.ToList());

        var entries = new List<DailyAqiEntry>();
        int lastKnownAqi = await GetFallbackAqiAsync(targetCity, cancellationToken);

        for (var date = startDate; date <= today; date = date.AddDays(1))
        {
            if (grouped.TryGetValue(date, out var dayRecords) && dayRecords.Count > 0)
            {
                lastKnownAqi = (int)Math.Round(dayRecords.Average(r => r.AqiValue));
            }

            var category = GetAqiCategory(lastKnownAqi);
            entries.Add(new DailyAqiEntry(
                Date: date.ToString("yyyy-MM-dd"),
                DayName: date.ToString("dddd"),
                ShortDay: date.ToString("ddd"),
                Aqi: lastKnownAqi,
                Category: category,
                Color: GetAqiColor(lastKnownAqi)
            ));
        }

        _logger.LogInformation("Generated daily AQI timeline for {City}", targetCity);

        return new DailyAqiResponse(
            City: targetCity,
            Period: "Last 7 days",
            Data: entries,
            Timestamp: DateTime.UtcNow
        );
    }

    private async Task<int> GetFallbackAqiAsync(string city, CancellationToken cancellationToken)
    {
        var latest = await _repository.GetMostRecentAsync(city, cancellationToken);
        if (latest is not null)
        {
            return latest.AqiValue;
        }

        try
        {
            var live = await _airQualityService.GetLiveAqiAsync(city, forceFresh: true, cancellationToken);
            return live.OverallAqi;
        }
        catch
        {
            return 75; // Moderate default
        }
    }

    private static string GetAqiCategory(int aqi) => aqi switch
    {
        <= 50 => "Good",
        <= 100 => "Moderate",
        <= 150 => "Unhealthy for Sensitive Groups",
        <= 200 => "Unhealthy",
        <= 300 => "Very Unhealthy",
        _ => "Hazardous"
    };

    private static string GetAqiColor(int aqi) => aqi switch
    {
        <= 50 => "#00e400",
        <= 100 => "#ffff00",
        <= 150 => "#ff7e00",
        <= 200 => "#ff0000",
        <= 300 => "#8f3f97",
        _ => "#7e0023"
    };
}
