using System.Text.Json;
using Microsoft.Extensions.Options;
using BosniaAir.Api.Configuration;
using BosniaAir.Api.Dtos;
using BosniaAir.Api.Enums;
using BosniaAir.Api.Repositories;
using BosniaAir.Api.Utilities;

namespace BosniaAir.Api.Services;

/// <summary>
/// Main interface for air quality operations - fetching live data, forecasts, and refreshing from WAQI.
/// </summary>
public interface IAirQualityService
{
    /// <summary>
    /// Gets the latest live AQI data for a city. Throws if no cached data exists.
    /// </summary>
    /// <example>var live = await service.GetLive(City.Sarajevo);</example>
    Task<LiveAqiResponse> GetLive(City city, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches forecast data for a city. Might throw if cache is empty or corrupted.
    /// </summary>
    Task<ForecastResponse> GetForecast(City city, CancellationToken cancellationToken = default);

    /// <summary>
    /// Combines live and forecast into one call. Always returns live data, forecast might be empty.
    /// </summary>
    /// <example>var full = await service.GetComplete(City.Tuzla);</example>
    Task<CompleteAqiResponse> GetComplete(
        City city,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a refresh from the WAQI API for the given city.
    /// </summary>
    Task RefreshCity(City city, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles all air quality logic: API calls to WAQI, caching, and data transformation.
/// </summary>
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

    /// <summary>
    /// Sets up the service with HTTP client, repo, and WAQI token from config.
    /// </summary>
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

    /// <inheritdoc />
    public async Task<LiveAqiResponse> GetLive(City city, CancellationToken cancellationToken = default)
    {
        var cached = await _repository.GetLiveData(city, cancellationToken);
        if (cached is not null)
        {
            return AirQualityMapper.MapToLiveResponse(cached);
        }

        _logger.LogWarning("Live data requested for {City} but cache is empty", city);
        throw new DataUnavailableException(city, "live");
    }

    /// <inheritdoc />
    public async Task<ForecastResponse> GetForecast(City city, CancellationToken cancellationToken = default)
    {
        var cached = await _repository.GetForecast(city, cancellationToken);
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

    /// <inheritdoc />
    public async Task<CompleteAqiResponse> GetComplete(
        City city,
        CancellationToken cancellationToken = default)
    {
        var live = await GetLive(city, cancellationToken);
        ForecastResponse forecast;

        try
        {
            forecast = await GetForecast(city, cancellationToken);
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

    /// <inheritdoc />
    public Task RefreshCity(City city, CancellationToken cancellationToken = default)
    {
        return RefreshInternal(city, cancellationToken);
    }

    /// <summary>
    /// Internal refresh logic: fetches from WAQI, saves to DB, returns fresh data.
    /// </summary>
    private async Task<RefreshResult> RefreshInternal(City city, CancellationToken cancellationToken)
    {
        var waqiData = await FetchApiData(city, cancellationToken);
        var timestamp = AirQualityMapper.ParseTimestamp(waqiData.Time);

        var record = AirQualityMapper.MapToEntity(city, waqiData, timestamp);
        await _repository.AddLiveSnapshot(record, cancellationToken);
        
        var liveResponse = AirQualityMapper.MapToLiveResponse(record);

        ForecastResponse? forecastResponse = null;
        if (waqiData.Forecast?.Daily is not null)
        {
            forecastResponse = AirQualityMapper.BuildForecastResponse(city, waqiData.Forecast.Daily, timestamp);
            if (forecastResponse is not null)
            {
                var cachePayload = new ForecastCache(timestamp, forecastResponse.Forecast);
                var serialized = JsonSerializer.Serialize(cachePayload, CacheSerializerOptions);
                await _repository.UpdateForecast(city, serialized, timestamp, cancellationToken);
            }
        }

        return new RefreshResult(liveResponse, forecastResponse);
    }

    /// <summary>
    /// Hits the WAQI API for the city's station data. Validates response and deserializes.
    /// </summary>
    private async Task<WaqiData> FetchApiData(City city, CancellationToken cancellationToken)
    {
        var stationId = city.ToStationId();
        var requestUri = $"feed/{stationId}/?token={_apiToken}";

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

    /// <summary>
    /// Safely deserializes cached forecast JSON. Returns null on failure.
    /// </summary>
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

    /// <summary>
    /// Internal result of a refresh operation, containing both live and forecast data.
    /// </summary>
    private sealed record RefreshResult(LiveAqiResponse Live, ForecastResponse? Forecast);

    /// <summary>
    /// Cached forecast data with retrieval timestamp.
    /// </summary>
    private sealed record ForecastCache(DateTime RetrievedAt, IReadOnlyList<ForecastDayDto> Days);
}
