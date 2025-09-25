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
        // Za sada vraćamo basic test response
        // TODO: Implementiraj WAQI API poziv
        return new LiveAqiResponse(
            City: cityName,
            OverallAqi: 75,
            AqiCategory: "Moderate",
            Color: "#FFFF00",
            HealthMessage: "Air quality is acceptable for most people",
            Timestamp: DateTime.UtcNow,
            Measurements: Array.Empty<MeasurementDto>(),
            DominantPollutant: "PM2.5"
        );
    }
}
