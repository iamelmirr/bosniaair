using Microsoft.Extensions.Logging;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Models;

namespace SarajevoAir.Api.Services;

public interface IForecastService
{
    Task<ForecastResponse> GetForecastAsync(string city, bool forceFresh = false, CancellationToken cancellationToken = default);
}

public class ForecastService : IForecastService
{
    private readonly IAqicnClient _aqicnClient;
    private readonly AirQualityCache _cache;
    private readonly ILogger<ForecastService> _logger;

    private static readonly TimeSpan ForecastTtl = TimeSpan.FromHours(2);

    public ForecastService(
        IAqicnClient aqicnClient,
        AirQualityCache cache,
        ILogger<ForecastService> logger)
    {
        _aqicnClient = aqicnClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ForecastResponse> GetForecastAsync(string city, bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        var targetCity = string.IsNullOrWhiteSpace(city) ? "Sarajevo" : city.Trim();

        if (!forceFresh && targetCity.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase) &&
            _cache.TryGetForecast(targetCity, ForecastTtl, out var cacheEntry))
        {
            _logger.LogDebug("Returning cached forecast for {City}", targetCity);
            return cacheEntry.Response;
        }

        var apiResponse = await _aqicnClient.GetCityDataAsync(targetCity, cancellationToken);
        if (apiResponse?.Data?.Forecast?.Daily is null)
        {
            _logger.LogWarning("Forecast data missing for {City}", targetCity);
            return new ForecastResponse(targetCity, Array.Empty<ForecastDayDto>(), DateTime.UtcNow);
        }

        var forecast = BuildForecast(apiResponse.Data.Forecast.Daily, targetCity);
        var response = new ForecastResponse(targetCity, forecast, DateTime.UtcNow);
        _cache.SetForecast(targetCity, new AirQualityCache.ForecastEntry(response));
        return response;
    }

    private static IReadOnlyList<ForecastDayDto> BuildForecast(AqicnDailyForecast dailyForecast, string city)
    {
        var days = new List<ForecastDayDto>();
        var pm25 = dailyForecast.Pm25 ?? Array.Empty<AqicnDayForecast>();

        foreach (var entry in pm25.Take(7))
        {
            var date = entry.Day;
            var aqi = CalculateAqiFromPm25(entry.Avg);
            var pm10 = dailyForecast.Pm10?.FirstOrDefault(x => x.Day == date);
            var o3 = dailyForecast.O3?.FirstOrDefault(x => x.Day == date);

            days.Add(new ForecastDayDto(
                Date: date,
                Aqi: aqi,
                Category: GetAqiCategory(aqi),
                Color: GetAqiColor(aqi),
                Pollutants: new ForecastDayPollutants(
                    Pm25: new PollutantRangeDto(entry.Avg, entry.Min, entry.Max),
                    Pm10: pm10 is null ? null : new PollutantRangeDto(pm10.Avg, pm10.Min, pm10.Max),
                    O3: o3 is null ? null : new PollutantRangeDto(o3.Avg, o3.Min, o3.Max)
                )
            ));
        }

        return days;
    }

    private static int CalculateAqiFromPm25(double pm25)
    {
        if (pm25 <= 12.0) return (int)Math.Round((50.0 / 12.0) * pm25);
        if (pm25 <= 35.4) return (int)Math.Round(((100 - 51) / (35.4 - 12.1)) * (pm25 - 12.1) + 51);
        if (pm25 <= 55.4) return (int)Math.Round(((150 - 101) / (55.4 - 35.5)) * (pm25 - 35.5) + 101);
        if (pm25 <= 150.4) return (int)Math.Round(((200 - 151) / (150.4 - 55.5)) * (pm25 - 55.5) + 151);
        if (pm25 <= 250.4) return (int)Math.Round(((300 - 201) / (250.4 - 150.5)) * (pm25 - 150.5) + 201);
        return (int)Math.Round(((500 - 301) / (500.4 - 250.5)) * (pm25 - 250.5) + 301);
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
        <= 50 => "#22C55E",
        <= 100 => "#EAB308",
        <= 150 => "#F97316",
        <= 200 => "#EF4444",
        <= 300 => "#A855F7",
        _ => "#7C2D12"
    };
}
