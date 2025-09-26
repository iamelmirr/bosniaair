using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Repositories;
using SarajevoAir.Api.Entities;
using SarajevoAir.Api.Utilities;
using Microsoft.Extensions.Logging;
using AutoMapper;

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
    private readonly IMapper _mapper; // 🆕 AutoMapper za object mapping

    public SarajevoService(IAqiRepository repository, ILogger<SarajevoService> logger, IMapper mapper)
    {
        _repository = repository;
        _logger = logger;
        _mapper = mapper; // 🆕 Dependency Injection omogućava AutoMapper
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
                Timestamp: TimeZoneHelper.GetSarajevoTime(),
                Measurements: new List<MeasurementDto>(),
                DominantPollutant: "Unknown"
            );
        }

        // � AUTOMAPPER: Osnovni mapping SarajevoMeasurement → LiveAqiResponse
        var response = _mapper.Map<LiveAqiResponse>(measurement);
        
        // ✨ Custom logic: AQI kategorija, boja i zdravstvena poruka
        var aqiFromDb = measurement.AqiValue ?? 0;
        var (_, category, color, message) = GetAqiInfo(aqiFromDb);
        
        // 🎨 Postavi custom properties koje AutoMapper ne može automatski
        response = response with 
        {
            AqiCategory = category,
            Color = color,
            HealthMessage = message,
            Measurements = ConvertAllMeasurementsToDto(measurement) // Ovo ćemo refaktorisati sledeće!
        };

        return response;
    }

    public async Task<ForecastResponse> GetForecastAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        var forecasts = await _repository.GetSarajevoForecastAsync(5);
        if (!forecasts.Any())
        {
            return new ForecastResponse(
                City: "Sarajevo",
                Forecast: new List<ForecastDayDto>(),
                Timestamp: TimeZoneHelper.GetSarajevoTime()
            );
        }

        // 🚀 AUTOMAPPER: Mapiranje SarajevoForecast → ForecastDayDto
        var forecastDtos = forecasts.Select(f => {
            // Osnovni mapping pomoću AutoMapper-a
            var dto = _mapper.Map<ForecastDayDto>(f);
            
            // Custom logic za AQI kategoriju i boju (AutoMapper ne može ovo automatski)
            var aqiValue = (int)(f.Pm25Avg ?? 0);
            var (_, category, color, _) = GetAqiInfo(aqiValue);
            
            return dto with { Category = category, Color = color };
        }).ToList();

        _logger.LogInformation("Retrieved {Count} forecast days for Sarajevo", forecastDtos.Count);

        return new ForecastResponse(
            City: "Sarajevo", 
            Forecast: forecastDtos,
            Timestamp: TimeZoneHelper.GetSarajevoTime()
        );
    }

    public async Task<CompleteAqiResponse> GetCompleteAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        var live = await GetLiveAsync(forceFresh, cancellationToken);
        var forecast = await GetForecastAsync(forceFresh, cancellationToken);

        return new CompleteAqiResponse(
            LiveData: live,
            ForecastData: forecast,
            RetrievedAt: TimeZoneHelper.GetSarajevoTime()
        );
    }

    /// <summary>
    /// 🚀 REFAKTORISANO: Data-driven approach umjesto 100+ linija repetitivnog koda
    /// Koristi dictionary za definisanje svih pollutants u 1 mjestu
    /// </summary>
    private List<MeasurementDto> ConvertAllMeasurementsToDto(SarajevoMeasurement measurement)
    {
        var measurements = new List<MeasurementDto>();
        
        // 📊 Data-driven definicija svih pollutants (umjesto copy-paste kod)
        var pollutantDefinitions = new Dictionary<string, (double? value, string unit)>
        {
            { "PM2.5", (measurement.Pm25, "μg/m³") },
            { "PM10", (measurement.Pm10, "μg/m³") },
            { "O3", (measurement.O3, "μg/m³") },
            { "NO2", (measurement.No2, "μg/m³") },
            { "CO", (measurement.Co, "mg/m³") },
            { "SO2", (measurement.So2, "μg/m³") }
        };

        // 🔄 Loop kroz sve pollutante umjesto copy-paste
        foreach (var (parameter, (value, unit)) in pollutantDefinitions)
        {
            if (value.HasValue)
            {
                measurements.Add(new MeasurementDto(
                    Id: $"{measurement.Id}_{parameter.ToLower().Replace(".", "")}",
                    City: "Sarajevo",
                    LocationName: "Sarajevo Center",
                    Parameter: parameter,
                    Value: value.Value,
                    Unit: unit,
                    Timestamp: measurement.Timestamp,
                    SourceName: "WAQI",
                    Coordinates: null,
                    AveragingPeriod: null
                ));
            }
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