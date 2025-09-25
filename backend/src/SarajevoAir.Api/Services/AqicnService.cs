using System.Text.Json;
using SarajevoAir.Api.Dtos;

namespace SarajevoAir.Api.Services;

public interface IAqicnService
{
    Task<CityComparisonResponse> GetCitiesComparisonAsync();
    Task<LiveAqiResponse> GetCityLiveAsync(string cityName, CancellationToken cancellationToken = default);
}

public class AqicnService : IAqicnService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AqicnService> _logger;

    public AqicnService(HttpClient httpClient, ILogger<AqicnService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CityComparisonResponse> GetCitiesComparisonAsync()
    {
        // Za sada vraćamo basic test response
        // TODO: Implementiraj WAQI API poziv za comparison
        return new CityComparisonResponse(
            Cities: Array.Empty<CityComparisonEntry>(),
            ComparedAt: DateTime.UtcNow,
            TotalCities: 0
        );
    }

    public async Task<LiveAqiResponse> GetCityLiveAsync(string cityName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching real WAQI data for city: {CityName}", cityName);

        // WAQI API station IDs za različite gradove
        var stationId = cityName?.ToLowerInvariant() switch
        {
            "tuzla" => "A70318",
            "zenica" => "@9267", // Zenica Centar station
            "mostar" => "@14726",
            "banja luka" => "A84268",
            "bihac" => "@13578",
            "sarajevo" => "@10557", // Sarajevo US Embassy - main monitoring station
            _ => throw new ArgumentException($"No WAQI station configured for city: {cityName}")
        };

        try
        {
            var apiToken = "4017a1c616179160829bd7e3abb9cc9c8449958e";
            var apiUrl = $"https://api.waqi.info/feed/{stationId}/?token={apiToken}";
            
            _logger.LogDebug("Calling WAQI API: {ApiUrl}", apiUrl);
            
            var response = await _httpClient.GetStringAsync(apiUrl, cancellationToken);
            var waqiResponse = System.Text.Json.JsonSerializer.Deserialize<WaqiApiResponse>(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (waqiResponse?.Status != "ok" || waqiResponse.Data == null)
            {
                throw new InvalidOperationException($"WAQI API returned invalid response for {cityName}");
            }

            var data = waqiResponse.Data;
            
            // Skip detailed measurements for now - focus on main AQI value
            // TODO: Implement proper MeasurementDto if detailed pollutant data is needed

            // Calculate AQI category and color from numeric AQI
            var (category, color, healthMessage) = GetAqiInfo(data.Aqi);

            var timestamp = DateTime.TryParse(data.Time.Iso, out var parsedTime) ? parsedTime : DateTime.UtcNow;

            _logger.LogInformation("Successfully retrieved WAQI data for {CityName}, AQI: {Aqi}", cityName, data.Aqi);

            return new LiveAqiResponse(
                City: cityName,
                OverallAqi: data.Aqi,
                AqiCategory: category,
                Color: color,
                HealthMessage: healthMessage,
                Timestamp: timestamp,
                Measurements: Array.Empty<MeasurementDto>(),
                DominantPollutant: MapDominantPollutant(data.Dominentpol)
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching WAQI data for {CityName}", cityName);
            throw new InvalidOperationException($"Failed to fetch air quality data for {cityName}: Network error", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while processing WAQI data for {CityName}", cityName);
            throw new InvalidOperationException($"Failed to parse air quality data for {cityName}: Invalid data format", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching WAQI data for {CityName}", cityName);
            throw;
        }
    }

    private static (string Category, string Color, string HealthMessage) GetAqiInfo(int aqi)
    {
        return aqi switch
        {
            <= 50 => ("Good", "#00E400", "Air quality is considered satisfactory, and air pollution poses little or no risk."),
            <= 100 => ("Moderate", "#FFFF00", "Air quality is acceptable for most people. However, for some pollutants there may be a moderate health concern for a very small number of people who are unusually sensitive to air pollution."),
            <= 150 => ("Unhealthy for Sensitive Groups", "#FF7E00", "Members of sensitive groups may experience health effects. The general public is not likely to be affected."),
            <= 200 => ("Unhealthy", "#FF0000", "Everyone may begin to experience health effects; members of sensitive groups may experience more serious health effects."),
            <= 300 => ("Very Unhealthy", "#8F3F97", "Health warnings of emergency conditions. The entire population is more likely to be affected."),
            _ => ("Hazardous", "#7E0023", "Health alert: everyone may experience more serious health effects.")
        };
    }

    private static string MapDominantPollutant(string? pollutant)
    {
        return pollutant?.ToLowerInvariant() switch
        {
            "pm25" => "PM2.5",
            "pm10" => "PM10",
            "no2" => "NO2",
            "o3" => "O3",
            "so2" => "SO2",
            "co" => "CO",
            _ => "Unknown"
        };
    }
}
