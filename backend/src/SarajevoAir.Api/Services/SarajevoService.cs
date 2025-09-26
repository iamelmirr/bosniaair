using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Repositories;
using SarajevoAir.Api.Entities;
using Microsoft.Extensions.Logging;

namespace SarajevoAir.Api.Services;

public interface ISarajevoService
{
    Task<LiveAqiResponse> GetLiveAsync(bool forceFresh = false, CancellationToken cancellationToken = default);
    Task<ForecastResponse> GetForecastAsync(bool forceFresh = false, CancellationToken cancellationToken = default);
    Task<CompleteAqiResponse> GetCompleteAsync(bool forceFresh = false, CancellationToken cancellationToken = default);
}

public class SarajevoService : ISarajevoService
{
    private readonly IAqiRepository _repository;
    private readonly ILogger<SarajevoService> _logger;

    public SarajevoService(IAqiRepository repository, ILogger<SarajevoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<LiveAqiResponse> GetLiveAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        var measurement = await _repository.GetLatestSarajevoMeasurementAsync();
        if (measurement == null)
        {
            return new LiveAqiResponse(
                City: "Sarajevo",
                OverallAqi: 0,
                AqiCategory: "No Data",
                Color: "#999999",
                HealthMessage: "Data not available",
                Timestamp: DateTime.UtcNow,
                Measurements: new List<MeasurementDto>(),
                DominantPollutant: "Unknown"
            );
        }

        // 🚨 KRITIČNO: Koristi AQI direktno iz baze (već izračunat od WAQI API-ja)
        var aqiFromDb = measurement.AqiValue ?? 0;
        var (_, category, color, message) = GetAqiInfo(aqiFromDb); // Samo za kategoriju i boje
        var measurementDtos = ConvertAllMeasurementsToDto(measurement);

        return new LiveAqiResponse(
            City: "Sarajevo",
            OverallAqi: aqiFromDb, // 🚨 Koristi AQI iz baze, NE računaj!
            AqiCategory: category,
            Color: color,
            HealthMessage: message,
            Timestamp: measurement.Timestamp,
            Measurements: measurementDtos,
            DominantPollutant: "PM2.5"
        );
    }

    public async Task<ForecastResponse> GetForecastAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        var forecasts = await _repository.GetSarajevoForecastAsync(5);
        if (!forecasts.Any())
        {
            return new ForecastResponse(
                City: "Sarajevo",
                Forecast: new List<ForecastDayDto>(),
                Timestamp: DateTime.UtcNow
            );
        }

        var forecastDtos = forecasts.Select(f => {
            // Koristi PM2.5 average kao AQI vrednost (jednostavno mapiranje)
            var aqiValue = (int)(f.Pm25Avg ?? 0);
            var (_, category, color, _) = GetAqiInfo(aqiValue); // Koristi PM2.5 avg kao AQI
            return new ForecastDayDto(
                Date: f.Date.ToString("yyyy-MM-dd"),
                Aqi: aqiValue,
                Category: category,
                Color: color,
                Pollutants: new ForecastDayPollutants(
                    Pm25: new PollutantRangeDto((int)(f.Pm25Avg ?? 0), (int)(f.Pm25Min ?? 0), (int)(f.Pm25Max ?? 0)),
                    Pm10: null,
                    O3: null
                )
            );
        }).ToList();

        _logger.LogInformation("Retrieved {Count} forecast days for Sarajevo", forecastDtos.Count);

        return new ForecastResponse(
            City: "Sarajevo", 
            Forecast: forecastDtos,
            Timestamp: DateTime.UtcNow
        );
    }

    public async Task<CompleteAqiResponse> GetCompleteAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        var live = await GetLiveAsync(forceFresh, cancellationToken);
        var forecast = await GetForecastAsync(forceFresh, cancellationToken);

        return new CompleteAqiResponse(
            LiveData: live,
            ForecastData: forecast,
            RetrievedAt: DateTime.UtcNow
        );
    }

    // Pomoćna metoda za kreiranje SVIH MeasurementDto objekata iz SarajevoMeasurement
    private List<MeasurementDto> ConvertAllMeasurementsToDto(SarajevoMeasurement measurement)
    {
        var measurements = new List<MeasurementDto>();

        // PM2.5
        if (measurement.Pm25.HasValue)
        {
            measurements.Add(new MeasurementDto(
                Id: $"{measurement.Id}_pm25",
                City: "Sarajevo",
                LocationName: "Sarajevo Center",
                Parameter: "PM2.5",
                Value: measurement.Pm25.Value,
                Unit: "μg/m³",
                Timestamp: measurement.Timestamp,
                SourceName: "WAQI",
                Coordinates: null,
                AveragingPeriod: null
            ));
        }

        // PM10
        if (measurement.Pm10.HasValue)
        {
            measurements.Add(new MeasurementDto(
                Id: $"{measurement.Id}_pm10",
                City: "Sarajevo",
                LocationName: "Sarajevo Center", 
                Parameter: "PM10",
                Value: measurement.Pm10.Value,
                Unit: "μg/m³",
                Timestamp: measurement.Timestamp,
                SourceName: "WAQI",
                Coordinates: null,
                AveragingPeriod: null
            ));
        }

        // O3 (Ozone)
        if (measurement.O3.HasValue)
        {
            measurements.Add(new MeasurementDto(
                Id: $"{measurement.Id}_o3",
                City: "Sarajevo",
                LocationName: "Sarajevo Center",
                Parameter: "O3",
                Value: measurement.O3.Value,
                Unit: "μg/m³",
                Timestamp: measurement.Timestamp,
                SourceName: "WAQI",
                Coordinates: null,
                AveragingPeriod: null
            ));
        }

        // NO2 (Nitrogen Dioxide)
        if (measurement.No2.HasValue)
        {
            measurements.Add(new MeasurementDto(
                Id: $"{measurement.Id}_no2",
                City: "Sarajevo",
                LocationName: "Sarajevo Center",
                Parameter: "NO2",
                Value: measurement.No2.Value,
                Unit: "μg/m³",
                Timestamp: measurement.Timestamp,
                SourceName: "WAQI",
                Coordinates: null,
                AveragingPeriod: null
            ));
        }

        // CO (Carbon Monoxide) - zadržati originalnu mg/m³ jedinicu
        if (measurement.Co.HasValue)
        {
            measurements.Add(new MeasurementDto(
                Id: $"{measurement.Id}_co",
                City: "Sarajevo",
                LocationName: "Sarajevo Center",
                Parameter: "CO",
                Value: measurement.Co.Value, // Zadržati mg/m³ vrednost
                Unit: "mg/m³",
                Timestamp: measurement.Timestamp,
                SourceName: "WAQI",
                Coordinates: null,
                AveragingPeriod: null
            ));
        }

        // SO2 (Sulfur Dioxide)
        if (measurement.So2.HasValue)
        {
            measurements.Add(new MeasurementDto(
                Id: $"{measurement.Id}_so2",
                City: "Sarajevo",
                LocationName: "Sarajevo Center",
                Parameter: "SO2",
                Value: measurement.So2.Value,
                Unit: "μg/m³",
                Timestamp: measurement.Timestamp,
                SourceName: "WAQI",
                Coordinates: null,
                AveragingPeriod: null
            ));
        }

        return measurements;
    }

    // Pomoćna metoda za AQI kategoriju i boje (prima gotov AQI iz baze)
    private (int aqi, string category, string color, string message) GetAqiInfo(int aqiValue)
    {
        return aqiValue switch
        {
            <= 50 => (aqiValue, "Good", "#00E400", "Air quality is good"),
            <= 100 => (aqiValue, "Moderate", "#FFFF00", "Air quality is moderate"),
            <= 150 => (aqiValue, "Unhealthy for Sensitive Groups", "#FF7E00", "Unhealthy for sensitive groups"),
            <= 200 => (aqiValue, "Unhealthy", "#FF0000", "Air quality is unhealthy"),
            <= 300 => (aqiValue, "Very Unhealthy", "#8F3F97", "Air quality is very unhealthy"),
            _ => (aqiValue, "Hazardous", "#7E0023", "Air quality is hazardous")
        };
    }
}