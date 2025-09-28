using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SarajevoAir.Api.Configuration;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Entities;
using SarajevoAir.Api.Enums;
using SarajevoAir.Api.Repositories;
using SarajevoAir.Api.Utilities;

namespace SarajevoAir.Api.Services;

public interface IAirQualityService
{
    Task<LiveAqiResponse> GetLiveAsync(City city, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LiveAqiResponse>> GetHistoryAsync(City city, int limit, CancellationToken cancellationToken = default);
    Task<ForecastResponse> GetForecastAsync(City city, CancellationToken cancellationToken = default);
    Task<CompleteAqiResponse> GetCompleteAsync(
        City city,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<City, LiveAqiResponse>> GetLatestSnapshotsAsync(IEnumerable<City> cities, CancellationToken cancellationToken = default);
    Task RefreshCityAsync(City city, CancellationToken cancellationToken = default);
}

public class AirQualityService : IAirQualityService
{
    private static readonly JsonSerializerOptions CacheSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IAirQualityRepository _repository;
    private readonly ILogger<AirQualityService> _logger;
    private readonly string _apiToken;

    public AirQualityService(
        HttpClient httpClient,
        IAirQualityRepository repository,
        IOptions<AqicnConfiguration> aqicnOptions,
        ILogger<AirQualityService> logger)
    {
        _httpClient = httpClient;
        _repository = repository;
        _logger = logger;
        _apiToken = aqicnOptions.Value.ApiToken;

        if (string.IsNullOrWhiteSpace(_apiToken))
        {
            throw new InvalidOperationException("WAQI API token is not configured. Set Aqicn:ApiToken in configuration.");
        }
    }

    public async Task<LiveAqiResponse> GetLiveAsync(City city, CancellationToken cancellationToken = default)
    {
        var cached = await _repository.GetLatestSnapshotAsync(city, cancellationToken);
        if (cached is not null)
        {
            return MapToLiveResponse(cached);
        }

        _logger.LogWarning("Live data requested for {City} but cache is empty", city);
        throw new DataUnavailableException(city, "live");
    }

    public async Task<IReadOnlyList<LiveAqiResponse>> GetHistoryAsync(City city, int limit, CancellationToken cancellationToken = default)
    {
        var records = await _repository.GetSnapshotHistoryAsync(city, limit, cancellationToken);
        return records.Select(MapToLiveResponse).ToList();
    }

    public async Task<ForecastResponse> GetForecastAsync(City city, CancellationToken cancellationToken = default)
    {
        var cached = await _repository.GetForecastAsync(city, cancellationToken);
        if (cached is not null)
        {
            var forecast = DeserializeForecastCache(cached.ForecastJson);
            if (forecast is not null)
            {
                return new ForecastResponse(
                    City: city.ToDisplayName(),
                    Forecast: forecast.Days,
                    Timestamp: forecast.RetrievedAt
                );
            }

            _logger.LogWarning("Forecast cache deserialization failed for {City}", city);
        }

        _logger.LogWarning("Forecast data requested for {City} but cache is empty", city);
        throw new DataUnavailableException(city, "forecast");
    }

    public async Task<CompleteAqiResponse> GetCompleteAsync(
        City city,
        CancellationToken cancellationToken = default)
    {
        var live = await GetLiveAsync(city, cancellationToken);
        ForecastResponse forecast;

        try
        {
            forecast = await GetForecastAsync(city, cancellationToken);
        }
        catch (DataUnavailableException)
        {
            forecast = new ForecastResponse(
                City: city.ToDisplayName(),
                Forecast: Array.Empty<ForecastDayDto>(),
                Timestamp: TimeZoneHelper.GetSarajevoTime());
        }

        return new CompleteAqiResponse(live, forecast, TimeZoneHelper.GetSarajevoTime());
    }

    public async Task<IReadOnlyDictionary<City, LiveAqiResponse>> GetLatestSnapshotsAsync(IEnumerable<City> cities, CancellationToken cancellationToken = default)
    {
        var records = await _repository.GetLatestSnapshotsAsync(cities, cancellationToken);
        var mapped = records.ToDictionary(kvp => kvp.Key, kvp => MapToLiveResponse(kvp.Value));
        return new ReadOnlyDictionary<City, LiveAqiResponse>(mapped);
    }

    public Task RefreshCityAsync(City city, CancellationToken cancellationToken = default)
    {
        return RefreshInternalAsync(city, cancellationToken);
    }

    private async Task<RefreshResult> RefreshInternalAsync(City city, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing air quality data for {City}", city);

        var waqiData = await FetchWaqiDataAsync(city, cancellationToken);
        var timestamp = ParseTimestamp(waqiData.Time);

        var record = new AirQualityRecord
        {
            City = city,
            StationId = city.ToStationId(),
            RecordType = AirQualityRecordType.LiveSnapshot,
            Timestamp = timestamp,
            AqiValue = waqiData.Aqi,
            DominantPollutant = MapDominantPollutant(waqiData.Dominentpol),
            Pm25 = waqiData.Iaqi?.Pm25?.V,
            Pm10 = waqiData.Iaqi?.Pm10?.V,
            O3 = waqiData.Iaqi?.O3?.V,
            No2 = waqiData.Iaqi?.No2?.V,
            Co = waqiData.Iaqi?.Co?.V,
            So2 = waqiData.Iaqi?.So2?.V,
            CreatedAt = TimeZoneHelper.GetSarajevoTime()
        };

        await _repository.AddLiveSnapshotAsync(record, cancellationToken);
        var liveResponse = MapToLiveResponse(record);

        ForecastResponse? forecastResponse = null;
        if (waqiData.Forecast?.Daily is not null)
        {
            var forecastDays = BuildForecastDays(waqiData.Forecast.Daily);
            if (forecastDays.Count > 0)
            {
                var cachePayload = new ForecastCache(timestamp, forecastDays);
                var serialized = JsonSerializer.Serialize(cachePayload, CacheSerializerOptions);
                await _repository.UpsertForecastAsync(city, serialized, timestamp, cancellationToken);

                forecastResponse = new ForecastResponse(city.ToDisplayName(), forecastDays, timestamp);
            }
        }

        return new RefreshResult(liveResponse, forecastResponse);
    }

    private async Task<WaqiData> FetchWaqiDataAsync(City city, CancellationToken cancellationToken)
    {
        var stationId = city.ToStationId();
        var requestUri = $"feed/{stationId}/?token={_apiToken}";

        _logger.LogDebug("Calling WAQI API for {City} using station {StationId}", city, stationId);

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var waqiResponse = await JsonSerializer.DeserializeAsync<WaqiApiResponse>(stream, CacheSerializerOptions, cancellationToken);

        if (waqiResponse is null)
        {
            throw new InvalidOperationException("Failed to deserialize WAQI API response");
        }

        if (!string.Equals(waqiResponse.Status, "ok", StringComparison.OrdinalIgnoreCase) || waqiResponse.Data is null)
        {
            throw new InvalidOperationException($"WAQI API returned invalid status '{waqiResponse.Status}' for city {city}");
        }

        return waqiResponse.Data;
    }

    private static DateTime ParseTimestamp(WaqiTime time)
    {
        if (!string.IsNullOrWhiteSpace(time.Iso) && DateTime.TryParse(time.Iso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedIso))
        {
            return TimeZoneHelper.ConvertToSarajevoTime(parsedIso.ToUniversalTime());
        }

        if (time.V > 0)
        {
            var unixEpoch = DateTimeOffset.FromUnixTimeSeconds(time.V);
            return TimeZoneHelper.ConvertToSarajevoTime(unixEpoch.UtcDateTime);
        }

        return TimeZoneHelper.GetSarajevoTime();
    }

    private static LiveAqiResponse MapToLiveResponse(AirQualityRecord record)
    {
        var aqi = record.AqiValue ?? 0;
        var (category, color, message) = GetAqiInfo(aqi);
        var measurements = BuildMeasurements(record);

        return new LiveAqiResponse(
            City: record.City.ToDisplayName(),
            OverallAqi: aqi,
            AqiCategory: category,
            Color: color,
            HealthMessage: message,
            Timestamp: record.Timestamp,
            Measurements: measurements,
            DominantPollutant: record.DominantPollutant ?? "Unknown"
        );
    }

    private static IReadOnlyList<MeasurementDto> BuildMeasurements(AirQualityRecord record)
    {
        var measurements = new List<MeasurementDto>();

        void AddMeasurement(string parameter, double? value, string unit)
        {
            if (!value.HasValue)
            {
                return;
            }

            measurements.Add(new MeasurementDto(
                Id: $"{record.Id}_{parameter.ToLowerInvariant()}",
                City: record.City.ToDisplayName(),
                LocationName: record.City.ToDisplayName(),
                Parameter: parameter,
                Value: value.Value,
                Unit: unit,
                Timestamp: record.Timestamp,
                SourceName: "WAQI",
                Coordinates: null,
                AveragingPeriod: null
            ));
        }

        AddMeasurement("PM2.5", record.Pm25, "μg/m³");
        AddMeasurement("PM10", record.Pm10, "μg/m³");
        AddMeasurement("O3", record.O3, "μg/m³");
        AddMeasurement("NO2", record.No2, "μg/m³");
        AddMeasurement("CO", record.Co, "mg/m³");
        AddMeasurement("SO2", record.So2, "μg/m³");

        return measurements;
    }

    private static ForecastCache? DeserializeForecastCache(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ForecastCache>(json, CacheSerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    private static List<ForecastDayDto> BuildForecastDays(WaqiDailyForecast forecast)
    {
        var map = new Dictionary<DateOnly, ForecastDayData>();

        void MergeEntries(WaqiForecastEntry[]? entries, Action<ForecastDayData, PollutantRangeDto> assign)
        {
            if (entries is null)
            {
                return;
            }

            foreach (var entry in entries)
            {
                if (!DateOnly.TryParseExact(entry.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                {
                    continue;
                }

                if (!map.TryGetValue(day, out var data))
                {
                    data = new ForecastDayData();
                    map[day] = data;
                }

                var range = new PollutantRangeDto(
                    Avg: ToInt(entry.Avg),
                    Min: ToInt(entry.Min),
                    Max: ToInt(entry.Max)
                );

                assign(data, range);
            }
        }

        MergeEntries(forecast.Pm25, (data, range) => data.Pm25 = range);
        MergeEntries(forecast.Pm10, (data, range) => data.Pm10 = range);
        MergeEntries(forecast.O3, (data, range) => data.O3 = range);

        var ordered = map.OrderBy(kvp => kvp.Key).ToList();
        var sarajevoToday = DateOnly.FromDateTime(TimeZoneHelper.GetSarajevoTime());
        
        // Only show actual available forecast data from WAQI (no fallback)
        var results = new List<ForecastDayDto>();

        foreach (var (date, data) in ordered)
        {
            // Only include dates from today onwards that actually exist in WAQI data
            if (date >= sarajevoToday && results.Count < 5)
            {
                var representativeAqi = data.Pm25?.Avg ?? data.Pm10?.Avg ?? data.O3?.Avg ?? 0;
                var (category, color, _) = GetAqiInfo(representativeAqi);
                var forecastPollutants = new ForecastDayPollutants(data.Pm25, data.Pm10, data.O3);

                results.Add(new ForecastDayDto(
                    Date: date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Aqi: representativeAqi,
                    Category: category,
                    Color: color,
                    Pollutants: forecastPollutants
                ));
            }
        }

        return results;
    }

    private static ForecastDayData? ResolveForecastDay(IReadOnlyDictionary<DateOnly, ForecastDayData> map, IReadOnlyList<DateOnly> orderedKeys, DateOnly target)
    {
        if (map.TryGetValue(target, out var exact))
        {
            return exact;
        }

        ForecastDayData? futureMatch = null;
        ForecastDayData? pastMatch = null;

        foreach (var key in orderedKeys)
        {
            if (key >= target)
            {
                futureMatch = map[key];
                break;
            }
        }

        if (futureMatch is null)
        {
            for (var i = orderedKeys.Count - 1; i >= 0; i--)
            {
                var key = orderedKeys[i];
                if (key < target)
                {
                    pastMatch = map[key];
                    break;
                }
            }
        }

        return futureMatch ?? pastMatch;
    }

    private static int ToInt(double value) => Convert.ToInt32(Math.Round(value, MidpointRounding.AwayFromZero));

    private static (string Category, string Color, string Message) GetAqiInfo(int aqi) => aqi switch
    {
        <= 50 => ("Good", "#00E400", "Air quality is considered satisfactory, and air pollution poses little or no risk."),
        <= 100 => ("Moderate", "#FFFF00", "Air quality is acceptable for most people. However, for some pollutants there may be a moderate health concern for a very small number of people who are unusually sensitive to air pollution."),
        <= 150 => ("Unhealthy for Sensitive Groups", "#FF7E00", "Members of sensitive groups may experience health effects. The general public is not likely to be affected."),
        <= 200 => ("Unhealthy", "#FF0000", "Everyone may begin to experience health effects; members of sensitive groups may experience more serious health effects."),
        <= 300 => ("Very Unhealthy", "#8F3F97", "Health warnings of emergency conditions. The entire population is more likely to be affected."),
        _ => ("Hazardous", "#7E0023", "Health alert: everyone may experience more serious health effects.")
    };

    private static string MapDominantPollutant(string? pollutant) => pollutant?.ToLowerInvariant() switch
    {
        "pm25" => "PM2.5",
        "pm10" => "PM10",
        "no2" => "NO2",
        "o3" => "O3",
        "so2" => "SO2",
        "co" => "CO",
        _ => "Unknown"
    };

    private sealed record RefreshResult(LiveAqiResponse Live, ForecastResponse? Forecast);

    private sealed record ForecastCache(DateTime RetrievedAt, IReadOnlyList<ForecastDayDto> Days);

    private sealed class ForecastDayData
    {
        public PollutantRangeDto? Pm25 { get; set; }
        public PollutantRangeDto? Pm10 { get; set; }
        public PollutantRangeDto? O3 { get; set; }
    }
}
