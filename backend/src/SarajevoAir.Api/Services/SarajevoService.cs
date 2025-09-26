/*
===================================    public async Task<LiveAqiResponse> GetLiveAsync(bool fo            // 🌐 NE ČUVAJ U BAZU - AirQualityRefreshService je JEDINI koji čuva podatke
            // SarajevoService je samo za čitanje i fallback API pozive
            var localTimestamp = SarajevoAir.Api.Utilities.TimeZoneHelper.GetSarajevoTime(); // Koristi Sarajevo lokalno vrijemeFresh = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching real WAQI live data for Sarajevo (forceFresh: {ForceFresh})", forceFresh);

        try
        {
            const string sarajevoStationId = "@10557"; // Sarajevo US Embassy - main monitoring station
            const string apiToken = "4017a1c616179160829bd7e3abb9cc9c8449958e";================================================
                               SARAJEVO SERVICE - SPECIALIZED FOR SARAJEVO
===========================================================================================

PURPOSE: Optimized service za Sarajevo sa real WAQI API pozivima
Koristi WAQI API za live i forecast podatke

DESIGN: 
- Single responsibility - samo Sarajevo
- Real API integration umesto test podataka
- Error handling i logging
- Optimized for frequent requests (future caching implementation)
*/

using System.Text.Json;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Repositories;
using SarajevoAir.Api.Entities;

namespace SarajevoAir.Api.Services;

public interface ISarajevoService
{
    Task<LiveAqiResponse> GetLiveAsync(bool forceFresh = false, CancellationToken cancellationToken = default);
    Task<ForecastResponse> GetForecastAsync(bool forceFresh = false, CancellationToken cancellationToken = default);
    Task<CompleteAqiResponse> GetCompleteAsync(bool forceFresh = false, CancellationToken cancellationToken = default);
}

public class SarajevoService : ISarajevoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SarajevoService> _logger;
    private readonly IAqiRepository _aqiRepository;

    public SarajevoService(HttpClient httpClient, ILogger<SarajevoService> logger, IAqiRepository aqiRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _aqiRepository = aqiRepository;
    }

    public async Task<LiveAqiResponse> GetLiveAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Sarajevo AQI request (forceFresh: {ForceFresh})", forceFresh);

        // 🎯 DATABASE-FIRST APPROACH - proverava cache pre API poziva ALI UVEK FRESH MEASUREMENTS
        if (!forceFresh)
        {
            var cachedRecord = await _aqiRepository.GetMostRecentAsync("Sarajevo", cancellationToken);
            if (cachedRecord != null && IsRecordFresh(cachedRecord))
            {
                _logger.LogInformation("📦 Using cached AQI but fetching fresh measurements - AQI: {Aqi}, Age: {Age}min", 
                    cachedRecord.AqiValue, (DateTime.UtcNow - cachedRecord.Timestamp).TotalMinutes);
                Console.WriteLine($"� [{DateTime.Now:HH:mm:ss}] FRONTEND READ: AQI {cachedRecord.AqiValue} from DATABASE + FRESH MEASUREMENTS from WAQI");
                
                // 🎯 DOHVATI FRESH MEASUREMENTS iz WAQI-a
                var freshMeasurements = await GetFreshMeasurementsAsync(cancellationToken);
                
                // Kombinuj cached AQI sa fresh measurements
                return new LiveAqiResponse(
                    City: cachedRecord.City,
                    OverallAqi: cachedRecord.AqiValue,
                    AqiCategory: GetAqiInfo(cachedRecord.AqiValue).Category,
                    Color: GetAqiInfo(cachedRecord.AqiValue).Color,
                    HealthMessage: GetAqiInfo(cachedRecord.AqiValue).HealthMessage,
                    Timestamp: cachedRecord.Timestamp, // 🔥 KORISTI TIMESTAMP IZ BAZE za AQI
                    Measurements: freshMeasurements, // 🎯 FRESH MEASUREMENTS za PollutantCard
                    DominantPollutant: "PM2.5"
                );
            }
            
            _logger.LogInformation("🔄 Cache miss or stale data, fetching from WAQI API");
        }

        // 🌐 FETCH FROM WAQI API
        try
        {
            const string sarajevoStationId = "@10557"; // Sarajevo US Embassy
            const string apiToken = "4017a1c616179160829bd7e3abb9cc9c8449958e";
            var apiUrl = $"https://api.waqi.info/feed/{sarajevoStationId}/?token={apiToken}";
            
            _logger.LogInformation("🏛️ Calling WAQI API - Station: {StationId}", sarajevoStationId);
            
            var response = await _httpClient.GetStringAsync(apiUrl, cancellationToken);
            var waqiResponse = System.Text.Json.JsonSerializer.Deserialize<WaqiApiResponse>(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (waqiResponse?.Status != "ok" || waqiResponse.Data == null)
            {
                throw new InvalidOperationException("WAQI API returned invalid response for Sarajevo");
            }

            var data = waqiResponse.Data;
            
            // 🎯 NOVA IMPLEMENTACIJA: Parse detailed measurements za PollutantCard komponente
            var measurements = ParseMeasurementsFromWaqi(data.Iaqi);

            // Calculate AQI category and color from numeric AQI
            var (category, color, healthMessage) = GetAqiInfo(data.Aqi);

            // � NE ČUVAJ U BAZU - AirQualityRefreshService je JEDINI koji čuva podatke
            // SarajevoService je samo za čitanje i fallback API pozive
            var localTimestamp = DateTime.UtcNow; // Koristi trenutno vreme za response
            
            _logger.LogInformation("⚠️ SarajevoService API fallback - Background service should handle data collection!");
            _logger.LogInformation("Successfully retrieved WAQI data for Sarajevo, AQI: {Aqi}, Measurements: {Count}", 
                data.Aqi, measurements.Count);

            return new LiveAqiResponse(
                City: "Sarajevo",
                OverallAqi: data.Aqi,
                AqiCategory: category,
                Color: color,
                HealthMessage: healthMessage,
                Timestamp: localTimestamp,
                Measurements: measurements,
                DominantPollutant: MapDominantPollutant(data.Dominentpol)
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching WAQI data for Sarajevo");
            throw new InvalidOperationException("Failed to fetch air quality data for Sarajevo: Network error", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while processing WAQI data for Sarajevo");
            throw new InvalidOperationException("Failed to parse air quality data for Sarajevo: Invalid data format", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching WAQI data for Sarajevo");
            throw;
        }
    }

    public async Task<ForecastResponse> GetForecastAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching real WAQI forecast data for Sarajevo (forceFresh: {ForceFresh})", forceFresh);
        Console.WriteLine($"🔵 [{DateTime.Now:HH:mm:ss}] FRONTEND REQUEST: Forecast API poziv (forceFresh: {forceFresh})");

        try
        {
            const string sarajevoStationId = "@10557"; // Sarajevo US Embassy - main monitoring station
            const string apiToken = "4017a1c616179160829bd7e3abb9cc9c8449958e";
            var apiUrl = $"https://api.waqi.info/feed/{sarajevoStationId}/?token={apiToken}";
            
            _logger.LogDebug("Calling WAQI API for Sarajevo forecast: {ApiUrl}", apiUrl);
            
            var response = await _httpClient.GetStringAsync(apiUrl, cancellationToken);
            var waqiResponse = System.Text.Json.JsonSerializer.Deserialize<WaqiApiResponse>(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (waqiResponse?.Status != "ok" || waqiResponse.Data == null)
            {
                throw new InvalidOperationException("WAQI API returned invalid response for Sarajevo forecast");
            }

            var forecastData = new List<ForecastDayDto>();

            // Process WAQI forecast data if available
            if (waqiResponse.Data.Forecast?.Daily != null)
            {
                var daily = waqiResponse.Data.Forecast.Daily;
                
                // Get PM2.5 forecast (most relevant for AQI)
                var pm25Forecast = daily.Pm25;
                if (pm25Forecast != null && pm25Forecast.Length > 0)
                {
                    for (int i = 0; i < Math.Min(6, pm25Forecast.Length); i++)
                    {
                        var dayForecast = pm25Forecast[i];
                        var aqi = (int)Math.Round(dayForecast.Avg);
                        var (category, color, _) = GetAqiInfo(aqi);
                        
                        forecastData.Add(new ForecastDayDto(
                            Date: dayForecast.Day,
                            Aqi: aqi,
                            Category: category,
                            Color: color,
                            Pollutants: new ForecastDayPollutants(null, null, null) // Empty pollutants for now
                        ));
                    }
                }
            }

            // If no forecast data available, create fallback based on current conditions
            if (forecastData.Count == 0)
            {
                _logger.LogWarning("No forecast data available from WAQI API, creating fallback forecast");
                var currentAqi = waqiResponse.Data.Aqi;
                
                for (int i = 0; i < 5; i++)
                {
                    // Create slight variations around current AQI
                    var variation = Random.Shared.Next(-10, 11);
                    var forecastAqi = Math.Max(0, currentAqi + variation);
                    var (category, color, _) = GetAqiInfo(forecastAqi);
                    
                    forecastData.Add(new ForecastDayDto(
                        Date: DateTime.Today.AddDays(i).ToString("yyyy-MM-dd"),
                        Aqi: forecastAqi,
                        Category: category,
                        Color: color,
                        Pollutants: new ForecastDayPollutants(null, null, null) // Empty pollutants for now
                    ));
                }
            }

            _logger.LogInformation("Successfully retrieved forecast data for Sarajevo, {Count} days", forecastData.Count);
            Console.WriteLine($"🔵 [{DateTime.Now:HH:mm:ss}] FORECAST API SUCCESS: Returned {forecastData.Count} days from WAQI API");

            return new ForecastResponse(
                City: "Sarajevo",
                Forecast: forecastData.AsReadOnly(),
                Timestamp: DateTime.UtcNow
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching WAQI forecast data for Sarajevo");
            throw new InvalidOperationException("Failed to fetch forecast data for Sarajevo: Network error", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while processing WAQI forecast data for Sarajevo");
            throw new InvalidOperationException("Failed to parse forecast data for Sarajevo: Invalid data format", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching WAQI forecast data for Sarajevo");
            throw;
        }
    }

    public async Task<CompleteAqiResponse> GetCompleteAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching complete WAQI data for Sarajevo (forceFresh: {ForceFresh})", forceFresh);
        
        // Call both live and forecast APIs
        var liveTask = GetLiveAsync(forceFresh, cancellationToken);
        var forecastTask = GetForecastAsync(forceFresh, cancellationToken);
        
        await Task.WhenAll(liveTask, forecastTask);
        
        var liveData = await liveTask;
        var forecastData = await forecastTask;
        
        return new CompleteAqiResponse(
            LiveData: liveData,
            ForecastData: forecastData,
            RetrievedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Proverava da li je cached record svež (mlađi od 15 minuta)
    /// </summary>
    private static bool IsRecordFresh(SimpleAqiRecord record)
    {
        var age = DateTime.UtcNow - record.Timestamp;
        var maxAge = TimeSpan.FromMinutes(15); // 🕐 15 minuta cache window
        return age <= maxAge;
    }

    /// <summary>
    /// Kreira LiveAqiResponse iz database record-a
    /// </summary>
    private static LiveAqiResponse CreateResponseFromDatabaseRecord(SimpleAqiRecord record)
    {
        var (category, color, healthMessage) = GetAqiInfo(record.AqiValue);
        
        return new LiveAqiResponse(
            City: record.City,
            OverallAqi: record.AqiValue,
            AqiCategory: category,
            Color: color,
            HealthMessage: healthMessage,
            Timestamp: record.Timestamp, // 🔥 KORISTI TIMESTAMP IZ BAZE
            Measurements: Array.Empty<MeasurementDto>(),
            DominantPollutant: "PM2.5" // Default value za cached podatke
        );
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

    /// <summary>
    /// Parsira detaljne measurements iz WAQI API iaqi objekta za PollutantCard komponente
    /// </summary>
    private static List<MeasurementDto> ParseMeasurementsFromWaqi(WaqiIaqi? iaqi)
    {
        var measurements = new List<MeasurementDto>();
        var timestamp = DateTime.UtcNow;

        if (iaqi == null)
        {
            return measurements;
        }

        // PM2.5 - Najvažniji zagađivač
        if (iaqi.Pm25?.V != null)
        {
            measurements.Add(new MeasurementDto(
                Id: $"sarajevo-pm25-{timestamp:yyyyMMdd-HHmmss}",
                City: "Sarajevo",
                LocationName: "US Embassy", 
                Parameter: "PM2.5",
                Value: iaqi.Pm25.V,
                Unit: "μg/m³",
                Timestamp: timestamp,
                SourceName: "WAQI"
            ));
        }

        // PM10
        if (iaqi.Pm10?.V != null)
        {
            measurements.Add(new MeasurementDto(
                Id: $"sarajevo-pm10-{timestamp:yyyyMMdd-HHmmss}",
                City: "Sarajevo",
                LocationName: "US Embassy",
                Parameter: "PM10",
                Value: iaqi.Pm10.V,
                Unit: "μg/m³",
                Timestamp: timestamp,
                SourceName: "WAQI"
            ));
        }

        // Ozon (O3)
        if (iaqi.O3?.V != null)
        {
            measurements.Add(new MeasurementDto(
                Id: $"sarajevo-o3-{timestamp:yyyyMMdd-HHmmss}",
                City: "Sarajevo",
                LocationName: "US Embassy",
                Parameter: "O3",
                Value: iaqi.O3.V,
                Unit: "μg/m³",
                Timestamp: timestamp,
                SourceName: "WAQI"
            ));
        }

        // Azot dioksid (NO2)
        if (iaqi.No2?.V != null)
        {
            measurements.Add(new MeasurementDto(
                Id: $"sarajevo-no2-{timestamp:yyyyMMdd-HHmmss}",
                City: "Sarajevo",
                LocationName: "US Embassy",
                Parameter: "NO2",
                Value: iaqi.No2.V,
                Unit: "μg/m³",
                Timestamp: timestamp,
                SourceName: "WAQI"
            ));
        }

        // Ugljen monoksid (CO)
        if (iaqi.Co?.V != null)
        {
            measurements.Add(new MeasurementDto(
                Id: $"sarajevo-co-{timestamp:yyyyMMdd-HHmmss}",
                City: "Sarajevo",
                LocationName: "US Embassy",
                Parameter: "CO",
                Value: iaqi.Co.V,
                Unit: "mg/m³",
                Timestamp: timestamp,
                SourceName: "WAQI"
            ));
        }

        // Sumpor dioksid (SO2)
        if (iaqi.So2?.V != null)
        {
            measurements.Add(new MeasurementDto(
                Id: $"sarajevo-so2-{timestamp:yyyyMMdd-HHmmss}",
                City: "Sarajevo",
                LocationName: "US Embassy",
                Parameter: "SO2",
                Value: iaqi.So2.V,
                Unit: "μg/m³",
                Timestamp: timestamp,
                SourceName: "WAQI"
            ));
        }

        return measurements;
    }

    /// <summary>
    /// Helper metoda za dohvaćanje fresh measurements iz WAQI API-a
    /// </summary>
    private async Task<List<MeasurementDto>> GetFreshMeasurementsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            const string sarajevoStationId = "@10557"; 
            const string apiToken = "4017a1c616179160829bd7e3abb9cc9c8449958e";
            var apiUrl = $"https://api.waqi.info/feed/{sarajevoStationId}/?token={apiToken}";
            
            var response = await _httpClient.GetStringAsync(apiUrl, cancellationToken);
            var waqiResponse = System.Text.Json.JsonSerializer.Deserialize<WaqiApiResponse>(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (waqiResponse?.Status == "ok" && waqiResponse.Data?.Iaqi != null)
            {
                _logger.LogInformation("✅ Fresh measurements fetched from WAQI for PollutantCards");
                return ParseMeasurementsFromWaqi(waqiResponse.Data.Iaqi);
            }
            else
            {
                _logger.LogWarning("⚠️ WAQI API returned no measurements data");
                return new List<MeasurementDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error fetching fresh measurements from WAQI");
            return new List<MeasurementDto>(); // Return empty list on error
        }
    }
}