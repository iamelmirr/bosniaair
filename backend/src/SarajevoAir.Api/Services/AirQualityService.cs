using Microsoft.Extensions.Logging;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Models;
using SarajevoAir.Api.Repositories;

namespace SarajevoAir.Api.Services;

public interface IAirQualityService
{
    Task<LiveAqiResponse> GetLiveAqiAsync(string city, bool forceFresh = false, CancellationToken cancellationToken = default);
    Task PersistSarajevoSnapshotAsync(int aqi, CancellationToken cancellationToken = default);
}

public class AirQualityService : IAirQualityService
{
    private static readonly string[] PollutantOrder = ["pm25", "pm10", "o3", "no2", "so2", "co"];

    private readonly IAqicnClient _aqicnClient;
    private readonly IAqiRepository _repository;
    private readonly AirQualityCache _cache;
    private readonly ILogger<AirQualityService> _logger;

    private static readonly TimeSpan LiveTtl = TimeSpan.FromMinutes(10);

    public AirQualityService(
        IAqicnClient aqicnClient,
        IAqiRepository repository,
        AirQualityCache cache,
        ILogger<AirQualityService> logger)
    {
        _aqicnClient = aqicnClient;
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<LiveAqiResponse> GetLiveAqiAsync(string city, bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        var targetCity = string.IsNullOrWhiteSpace(city) ? "Sarajevo" : city.Trim();

        if (!forceFresh && targetCity.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase) &&
            _cache.TryGetLive(targetCity, LiveTtl, out var cachedEntry))
        {
            _logger.LogDebug("Returning cached live AQI for {City}", targetCity);
            return cachedEntry.Response;
        }

        var apiResponse = await _aqicnClient.GetCityDataAsync(targetCity, cancellationToken);
        if (apiResponse?.Data is null)
        {
            throw new InvalidOperationException($"No AQI data available for {targetCity}.");
        }

        var liveResponse = BuildLiveResponse(apiResponse.Data, targetCity);
        _cache.SetLive(targetCity, new AirQualityCache.LiveEntry(liveResponse));

        if (targetCity.Equals("Sarajevo", StringComparison.OrdinalIgnoreCase))
        {
            await PersistSarajevoSnapshotAsync(liveResponse.OverallAqi, cancellationToken);
        }

        return liveResponse;
    }

    public async Task PersistSarajevoSnapshotAsync(int aqi, CancellationToken cancellationToken = default)
    {
        var latest = await _repository.GetMostRecentAsync("Sarajevo", cancellationToken);
        if (latest is not null)
        {
            var minutesSince = (DateTime.UtcNow - latest.Timestamp).TotalMinutes;
            if (minutesSince < 5 && latest.AqiValue == aqi)
            {
                _logger.LogDebug("Skipping AQI snapshot save – recent record exists with same value {Aqi}", aqi);
                return;
            }
        }

        await _repository.AddRecordAsync(new Entities.SimpleAqiRecord
        {
            City = "Sarajevo",
            AqiValue = aqi,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);
        _logger.LogInformation("Saved Sarajevo AQI snapshot: {Aqi}", aqi);
    }

    private static LiveAqiResponse BuildLiveResponse(AqicnData data, string city)
    {
        var measurements = BuildMeasurements(data, city);
        var aqi = data.Aqi;
        return new LiveAqiResponse(
            City: data.City?.Name ?? city,
            OverallAqi: aqi,
            AqiCategory: GetAqiCategory(aqi),
            Color: GetAqiColor(aqi),
            HealthMessage: GetHealthMessage(aqi),
            Timestamp: DateTime.UtcNow,
            Measurements: measurements,
            DominantPollutant: string.IsNullOrWhiteSpace(data.DominentPol) ? PollutantOrder[0] : data.DominentPol
        );
    }

    private static IReadOnlyList<MeasurementDto> BuildMeasurements(AqicnData data, string city)
    {
        var result = new List<MeasurementDto>();
        var latitude = data.City?.Geo is { Length: > 0 } geoLat ? geoLat[0] : 43.8563;
        var longitude = data.City?.Geo is { Length: > 1 } geoLon ? geoLon[1] : 18.4131;

        void TryAddMeasurement(string parameter, AqicnMeasurement? measurement, string unit)
        {
            if (measurement is null)
            {
                return;
            }

            result.Add(new MeasurementDto(
                Id: Guid.NewGuid().ToString(),
                City: city,
                LocationName: data.City?.Name ?? "City Center",
                Parameter: parameter,
                Value: measurement.V,
                Unit: unit,
                Timestamp: DateTime.UtcNow,
                SourceName: "AQICN",
                Coordinates: new CoordinateDto(latitude, longitude)
            ));
        }

        TryAddMeasurement("pm25", data.Iaqi?.Pm25, "µg/m³");
        TryAddMeasurement("pm10", data.Iaqi?.Pm10, "µg/m³");
        TryAddMeasurement("o3", data.Iaqi?.O3, "µg/m³");
        TryAddMeasurement("no2", data.Iaqi?.No2, "µg/m³");
        TryAddMeasurement("so2", data.Iaqi?.So2, "µg/m³");
        TryAddMeasurement("co", data.Iaqi?.Co, "mg/m³");

        return result;
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

    private static string GetHealthMessage(int aqi) => aqi switch
    {
        <= 50 => "Air quality is considered satisfactory, and air pollution poses little or no risk.",
        <= 100 => "Air quality is acceptable; however, there may be some health concern for a very small number of people who are unusually sensitive to air pollution.",
        <= 150 => "Members of sensitive groups may experience health effects. The general public is not likely to be affected.",
        <= 200 => "Everyone may begin to experience health effects; members of sensitive groups may experience more serious health effects.",
        <= 300 => "Health warnings of emergency conditions. The entire population is more likely to be affected.",
        _ => "Health alert: everyone may experience more serious health effects."
    };
}
