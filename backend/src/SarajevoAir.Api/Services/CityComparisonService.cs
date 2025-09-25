using System.Linq;
using Microsoft.Extensions.Logging;
using SarajevoAir.Api.Dtos;

namespace SarajevoAir.Api.Services;

public interface ICityComparisonService
{
    Task<CityComparisonResponse> CompareCitiesAsync(string citiesParameter, CancellationToken cancellationToken = default);
}

public class CityComparisonService : ICityComparisonService
{
    private readonly IAirQualityService _airQualityService;
    private readonly ILogger<CityComparisonService> _logger;

    public CityComparisonService(IAirQualityService airQualityService, ILogger<CityComparisonService> logger)
    {
        _airQualityService = airQualityService;
        _logger = logger;
    }

    public async Task<CityComparisonResponse> CompareCitiesAsync(string citiesParameter, CancellationToken cancellationToken = default)
    {
        var cities = ParseCities(citiesParameter);
        var results = new List<CityComparisonEntry>();

        foreach (var city in cities)
        {
            try
            {
                var live = await _airQualityService.GetLiveAqiAsync(city, forceFresh: true, cancellationToken);
                results.Add(new CityComparisonEntry(
                    City: live.City,
                    Aqi: live.OverallAqi,
                    Category: live.AqiCategory,
                    Color: live.Color,
                    DominantPollutant: live.DominantPollutant,
                    Timestamp: live.Timestamp
                ));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch AQI for city {City}", city);
                results.Add(new CityComparisonEntry(
                    City: city,
                    Aqi: null,
                    Category: "No Data",
                    Color: "#cccccc",
                    DominantPollutant: null,
                    Timestamp: null,
                    Error: ex.Message
                ));
            }
        }

        return new CityComparisonResponse(
            Cities: results,
            ComparedAt: DateTime.UtcNow,
            TotalCities: results.Count
        );
    }

    private static IReadOnlyList<string> ParseCities(string citiesParameter)
    {
        if (string.IsNullOrWhiteSpace(citiesParameter))
        {
            return new[] { "Sarajevo" };
        }

        return citiesParameter
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
