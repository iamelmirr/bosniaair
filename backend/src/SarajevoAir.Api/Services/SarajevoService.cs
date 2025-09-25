using SarajevoAir.Api.Dtos;

namespace SarajevoAir.Api.Services;

public interface ISarajevoService
{
    Task<LiveAqiResponse> GetLiveAsync(bool forceFresh = false, CancellationToken cancellationToken = default);
    Task<ForecastResponse> GetForecastAsync(bool forceFresh = false, CancellationToken cancellationToken = default);
    Task<SarajevoCompleteDto> GetCompleteAsync(bool forceFresh = false, CancellationToken cancellationToken = default);
}

public record SarajevoCompleteDto(
    LiveAqiResponse LiveData,
    ForecastResponse ForecastData,
    DateTime Timestamp
);

public class SarajevoService : ISarajevoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SarajevoService> _logger;

    public SarajevoService(HttpClient httpClient, ILogger<SarajevoService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LiveAqiResponse> GetLiveAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        // Za sada vraćamo basic test response
        // TODO: Implementiraj WAQI API poziv
        return new LiveAqiResponse(
            City: "Sarajevo",
            OverallAqi: 85,
            AqiCategory: "Moderate",
            Color: "#FFFF00",
            HealthMessage: "Air quality is acceptable for most people",
            Timestamp: DateTime.UtcNow,
            Measurements: Array.Empty<MeasurementDto>(),
            DominantPollutant: "PM2.5"
        );
    }

    public async Task<ForecastResponse> GetForecastAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        // Za sada vraćamo test response sa nekoliko dana forecast podataka
        // TODO: Implementiraj WAQI API poziv
        var testForecast = new List<ForecastDayDto>
        {
            new ForecastDayDto(
                Date: DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                Aqi: 75,
                Category: "Moderate",
                Color: "#FFFF00",
                Pollutants: new ForecastDayPollutants(
                    Pm25: new PollutantRangeDto(35, 25, 45),
                    Pm10: new PollutantRangeDto(65, 50, 80),
                    O3: null
                )
            ),
            new ForecastDayDto(
                Date: DateTime.Today.AddDays(2).ToString("yyyy-MM-dd"),
                Aqi: 82,
                Category: "Moderate", 
                Color: "#FFFF00",
                Pollutants: new ForecastDayPollutants(
                    Pm25: new PollutantRangeDto(42, 30, 55),
                    Pm10: new PollutantRangeDto(70, 55, 85),
                    O3: null
                )
            )
        };

        return new ForecastResponse(
            City: "Sarajevo",
            Forecast: testForecast,
            Timestamp: DateTime.UtcNow
        );
    }

    public async Task<SarajevoCompleteDto> GetCompleteAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        var liveTask = GetLiveAsync(forceFresh, cancellationToken);
        var forecastTask = GetForecastAsync(forceFresh, cancellationToken);

        await Task.WhenAll(liveTask, forecastTask);

        return new SarajevoCompleteDto(
            await liveTask,
            await forecastTask,
            DateTime.UtcNow
        );
    }
}
