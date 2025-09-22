using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SarajevoAir.Application.Dtos;
using SarajevoAir.Application.Interfaces;
using SarajevoAir.Infrastructure.OpenAq.Models;

namespace SarajevoAir.Infrastructure.OpenAq;

public class OpenAqClient : IOpenAqClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAqClient> _logger;
    private readonly string _apiKey;

    public OpenAqClient(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAqClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["OPENAQ_API_KEY"] ?? throw new InvalidOperationException("OPENAQ_API_KEY is required");

        _httpClient.BaseAddress = new Uri("https://api.openaq.org/v3/");
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
    }

    public async Task<List<LocationDto>> GetLocationsAsync(
        double lat, 
        double lon, 
        int radiusKm, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"locations?coordinates={lat},{lon}&radius={radiusKm * 1000}&limit=100"; // Convert km to meters
            _logger.LogInformation("Fetching locations from OpenAQ: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<OpenAqResponse<OpenAqLocation>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (apiResponse?.Results == null)
            {
                _logger.LogWarning("No locations returned from OpenAQ API");
                return new List<LocationDto>();
            }

            var locations = apiResponse.Results
                .Where(l => !string.IsNullOrEmpty(l.Name))
                .Select(l => new LocationDto(
                    l.Id,
                    l.Name,
                    l.Coordinates?.Latitude,
                    l.Coordinates?.Longitude,
                    l.Country ?? "Unknown",
                    l.City
                ))
                .ToList();

            _logger.LogInformation("Retrieved {Count} locations from OpenAQ", locations.Count);
            return locations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch locations from OpenAQ");
            return new List<LocationDto>();
        }
    }

    public async Task<List<SensorDto>> GetSensorsForLocationAsync(
        string externalLocationId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"locations/{externalLocationId}/sensors?limit=100";
            _logger.LogInformation("Fetching sensors for location {LocationId} from OpenAQ", externalLocationId);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<OpenAqResponse<OpenAqSensor>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (apiResponse?.Results == null)
            {
                _logger.LogWarning("No sensors returned for location {LocationId}", externalLocationId);
                return new List<SensorDto>();
            }

            var sensors = apiResponse.Results
                .Where(s => s.Parameter != null)
                .Select(s => new SensorDto(
                    s.Id,
                    s.Name ?? $"Sensor {s.Id}",
                    externalLocationId,
                    s.Parameter!,
                    s.Unit ?? "µg/m³"
                ))
                .ToList();

            _logger.LogInformation("Retrieved {Count} sensors for location {LocationId}", sensors.Count, externalLocationId);
            return sensors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch sensors for location {LocationId}", externalLocationId);
            return new List<SensorDto>();
        }
    }

    public async Task<List<MeasurementDto>> GetMeasurementsForSensorAsync(
        long sensorId, 
        DateTime sinceUtc, 
        int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sinceIso = sinceUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var url = $"sensors/{sensorId}/measurements?date_from={sinceIso}&limit={limit}&sort=desc";
            _logger.LogInformation("Fetching measurements for sensor {SensorId} since {Since}", sensorId, sinceIso);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<OpenAqResponse<OpenAqMeasurement>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (apiResponse?.Results == null)
            {
                _logger.LogWarning("No measurements returned for sensor {SensorId}", sensorId);
                return new List<MeasurementDto>();
            }

            var measurements = apiResponse.Results
                .Where(m => m.DatetimeUtc.HasValue && m.Value.HasValue)
                .Select(m => new MeasurementDto(
                    sensorId,
                    m.DatetimeUtc!.Value,
                    m.Value!.Value,
                    m.Parameter ?? "unknown",
                    m.Unit ?? "µg/m³"
                ))
                .ToList();

            _logger.LogInformation("Retrieved {Count} measurements for sensor {SensorId}", measurements.Count, sensorId);
            return measurements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch measurements for sensor {SensorId}", sensorId);
            return new List<MeasurementDto>();
        }
    }
}