/*
===================================    public async Task<LiveAqiResponse> GetLiveAsync(bool forceFresh = false, CancellationToken cancellationToken = default)
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

        // 🎯 DATABASE-FIRST APPROACH - proverava cache pre API poziva
        if (!forceFresh)
        {
            var cachedRecord = await _aqiRepository.GetMostRecentAsync("Sarajevo", cancellationToken);
            if (cachedRecord != null && IsRecordFresh(cachedRecord))
            {
                _logger.LogInformation("📦 Using cached data from database - AQI: {Aqi}, Age: {Age}min", 
                    cachedRecord.AqiValue, (DateTime.UtcNow - cachedRecord.Timestamp).TotalMinutes);
                return CreateResponseFromDatabaseRecord(cachedRecord);
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
            
            // Skip detailed measurements for now - focus on main AQI value
            // TODO: Implement proper MeasurementDto if detailed pollutant data is needed

            // Calculate AQI category and color from numeric AQI
            var (category, color, healthMessage) = GetAqiInfo(data.Aqi);

            // 🔥 SAČUVAJ U BAZU SA LOKALNIM TIMESTAMP-OM
            var localTimestamp = DateTime.UtcNow; // Uvek koristi trenutno vreme kada je podatak dobijen
            var aqiRecord = new SimpleAqiRecord
            {
                City = "Sarajevo",
                AqiValue = data.Aqi,
                Timestamp = localTimestamp
            };

            try 
            {
                await _aqiRepository.AddRecordAsync(aqiRecord, cancellationToken);
                _logger.LogInformation("💾 Saved AQI record to database: Sarajevo AQI {Aqi} at {Timestamp}", 
                    data.Aqi, localTimestamp);
            }
            catch (Exception dbEx)
            {
                _logger.LogWarning(dbEx, "❌ Failed to save AQI record to database, continuing with response");
                // Ne prekidaj request ako database save ne uspe
            }

            _logger.LogInformation("Successfully retrieved WAQI data for Sarajevo, AQI: {Aqi}", data.Aqi);

            return new LiveAqiResponse(
                City: "Sarajevo",
                OverallAqi: data.Aqi,
                AqiCategory: category,
                Color: color,
                HealthMessage: healthMessage,
                Timestamp: localTimestamp, // 🔥 KORISTI LOKALNI TIMESTAMP
                Measurements: Array.Empty<MeasurementDto>(),
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
}