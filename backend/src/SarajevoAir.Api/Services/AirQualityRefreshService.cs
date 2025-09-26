/*
===========================================================================================
                           AIR QUALITY REFRESH BACKGROUND SERVICE  
===========================================================================================

PURPOSE: Background service za periodično osvježavanje AQI podataka
- Radi svakih 10 minuta (configurable)
- Poziva WAQI API za sve gradove
- Kešira podatke za performance
- Ne blokira HTTP request-e

ARCHITECTURE PATTERN: HostedService (ASP.NET Core background service)
*/

using SarajevoAir.Api.Services;
using SarajevoAir.Api.Repositories;
using SarajevoAir.Api.Entities;
using SarajevoAir.Api.Dtos;

namespace SarajevoAir.Api.Services;

/// <summary>
/// Background service koji osvježava AQI podatke svakih 10 minuta
/// </summary>
public class AirQualityRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AirQualityRefreshService> _logger;
    private readonly TimeSpan _refreshInterval;

    public AirQualityRefreshService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AirQualityRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        // Čita interval iz konfiguracije (default 10 minuta)
        var intervalMinutes = configuration.GetValue<int>("Worker:FetchIntervalMinutes", 10);
        _refreshInterval = TimeSpan.FromMinutes(intervalMinutes);
        
        _logger.LogInformation("AirQualityRefreshService initialized with {IntervalMinutes} minute interval", intervalMinutes);
    }

    /// <summary>
    /// Glavna background task metoda
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AirQualityRefreshService started");
        
        // 🚀 ODMAH POKRENI PRVI REFRESH - ne čekaj!
        _logger.LogInformation("🚀 Starting initial refresh immediately...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshAllCityData(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during scheduled refresh cycle: {Error}", ex.Message);
            }

            // Čeka sledeći ciklus (10 minuta)
            _logger.LogInformation("⏰ Next refresh in {IntervalMinutes} minutes", _refreshInterval.TotalMinutes);
            
            try
            {
                await Task.Delay(_refreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normalno gašenje aplikacije
                break;
            }
        }
        
        _logger.LogInformation("AirQualityRefreshService stopped");
    }

    /// <summary>
    /// Osvježava podatke za sve gradove
    /// </summary>
    private async Task RefreshAllCityData(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 Starting scheduled refresh of all city data");
        
        using var scope = _scopeFactory.CreateScope();
        
        try
        {
            var sarajevoService = scope.ServiceProvider.GetRequiredService<ISarajevoService>();
            var aqicnService = scope.ServiceProvider.GetRequiredService<IAqicnService>();

            // Lista gradova za refresh
            var cities = new[] { "Sarajevo", "Tuzla", "Zenica", "Mostar", "Banja Luka", "Bihac" };

            var tasks = new List<Task>();

            foreach (var city in cities)
            {
                if (city == "Sarajevo")
                {
                    // Za Sarajevo koristi specialized service
                    tasks.Add(RefreshSarajevoData(sarajevoService, cancellationToken));
                }
                else
                {
                    // Za ostale gradove koristi AQICN service
                    tasks.Add(RefreshCityData(aqicnService, city, cancellationToken));
                }
            }

            // Parallel execution za sve gradove
            await Task.WhenAll(tasks);

            _logger.LogInformation("✅ Successfully completed refresh cycle for all cities");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during refresh cycle");
        }
    }

    /// <summary>
    /// Osvježava Sarajevo podatke direktno (API + database save)
    /// </summary>
    private async Task RefreshSarajevoData(ISarajevoService sarajevoService, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🏙️ Refreshing Sarajevo data");
            
            using var scope = _scopeFactory.CreateScope();
            var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
            var aqiRepository = scope.ServiceProvider.GetRequiredService<IAqiRepository>();
            
            // 🌐 DIRECT WAQI API CALL (background service only)
            const string sarajevoStationId = "@10557"; // Sarajevo US Embassy
            const string apiToken = "4017a1c616179160829bd7e3abb9cc9c8449958e";
            var apiUrl = $"https://api.waqi.info/feed/{sarajevoStationId}/?token={apiToken}";
            
            _logger.LogInformation("🏛️ Background refresh calling WAQI API - Station: {StationId}", sarajevoStationId);
            
            var response = await httpClient.GetStringAsync(apiUrl, cancellationToken);
            var waqiResponse = System.Text.Json.JsonSerializer.Deserialize<WaqiApiResponse>(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (waqiResponse?.Status != "ok" || waqiResponse.Data == null)
            {
                throw new InvalidOperationException("WAQI API returned invalid response for Sarajevo");
            }

            var data = waqiResponse.Data;
            
            // 💾 SAVE TO DATABASE (background service responsibility)
            var sarajevoTimestamp = SarajevoAir.Api.Utilities.TimeZoneHelper.GetSarajevoTime();
            
            // Save main AQI record (existing functionality)
            var aqiRecord = new SimpleAqiRecord
            {
                City = "Sarajevo",
                AqiValue = data.Aqi,
                Timestamp = sarajevoTimestamp
            };
            await aqiRepository.AddRecordAsync(aqiRecord, cancellationToken);
            
            // 🆕 Save detailed measurements
            var measurementsRecord = new SarajevoMeasurement
            {
                Timestamp = DateTime.UtcNow,
                Pm25 = data.Iaqi?.Pm25?.V,
                Pm10 = data.Iaqi?.Pm10?.V,
                O3 = data.Iaqi?.O3?.V,
                No2 = data.Iaqi?.No2?.V,
                Co = data.Iaqi?.Co?.V,
                So2 = data.Iaqi?.So2?.V,
                AqiValue = data.Aqi  // 🚨 VAŽNO: Koristi AQI direktno iz WAQI API-ja!
            };
            await aqiRepository.AddSarajevoMeasurementAsync(measurementsRecord, cancellationToken);
            
            // 🆕 Fetch and save forecast data
            await RefreshSarajevoForecast(httpClient, aqiRepository, cancellationToken);
            
            _logger.LogInformation("💾 Background service saved AQI record: Sarajevo AQI {Aqi} at {Timestamp}", 
                data.Aqi, sarajevoTimestamp);
            _logger.LogInformation("💾 Background service saved measurements: PM2.5={PM25}, PM10={PM10}, O3={O3}, NO2={NO2}, CO={CO}, SO2={SO2}", 
                measurementsRecord.Pm25, measurementsRecord.Pm10, measurementsRecord.O3, 
                measurementsRecord.No2, measurementsRecord.Co, measurementsRecord.So2);
            
            _logger.LogInformation("✅ Sarajevo refreshed - AQI: {Aqi}", data.Aqi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error refreshing Sarajevo data: {Error}", ex.Message);
        }
    }

    /// <summary>
    /// Osvježava podatke za ostale gradove
    /// </summary>
    private async Task RefreshCityData(IAqicnService aqicnService, string city, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🏙️ Refreshing {City} data", city);
            
            var result = await aqicnService.GetCityLiveAsync(city, cancellationToken);
            
            _logger.LogInformation("✅ {City} refreshed - AQI: {Aqi}", city, result.OverallAqi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error refreshing {City} data", city);
        }
    }

    /// <summary>
    /// Osvježava forecast podatke za Sarajevo iz WAQI API-ja
    /// </summary>
    private async Task RefreshSarajevoForecast(HttpClient httpClient, IAqiRepository aqiRepository, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("📊 Refreshing Sarajevo forecast data");
            
            const string sarajevoStationId = "@10557"; // Sarajevo US Embassy
            const string apiToken = "4017a1c616179160829bd7e3abb9cc9c8449958e";
            var forecastApiUrl = $"https://api.waqi.info/feed/{sarajevoStationId}/?token={apiToken}";
            
            var forecastResponse = await httpClient.GetStringAsync(forecastApiUrl, cancellationToken);
            var waqiForecastResponse = System.Text.Json.JsonSerializer.Deserialize<WaqiApiResponse>(forecastResponse, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (waqiForecastResponse?.Status == "ok" && waqiForecastResponse.Data?.Forecast?.Daily?.Pm25 != null)
            {
                foreach (var forecastEntry in waqiForecastResponse.Data.Forecast.Daily.Pm25)
                {
                    var forecastRecord = new SarajevoForecast
                    {
                        Date = DateTime.Parse(forecastEntry.Day).Date,
                        Pm25Min = forecastEntry.Min,
                        Pm25Max = forecastEntry.Max, 
                        Pm25Avg = forecastEntry.Avg,
                        Aqi = CalculateAqiFromPm25(forecastEntry.Avg), // Use avg PM2.5 for AQI
                        CreatedAt = DateTime.UtcNow
                    };

                    await aqiRepository.UpsertSarajevoForecastAsync(forecastRecord, cancellationToken);
                    
                    _logger.LogInformation("📊 Saved forecast for {Date}: PM2.5 avg={Avg}, min={Min}, max={Max}", 
                        forecastRecord.Date.ToShortDateString(), forecastRecord.Pm25Avg, 
                        forecastRecord.Pm25Min, forecastRecord.Pm25Max);
                }
                
                _logger.LogInformation("✅ Successfully refreshed Sarajevo forecast data");
            }
            else
            {
                _logger.LogWarning("⚠️ No forecast data available in WAQI API response");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error refreshing Sarajevo forecast data: {Error}", ex.Message);
        }
    }

    /// <summary>
    /// Jednostavno konvertovanje PM2.5 vrednosti u AQI (gruba aproksimacija)
    /// </summary>
    private static int? CalculateAqiFromPm25(double? pm25)
    {
        if (!pm25.HasValue) return null;
        
        var pm = pm25.Value;
        
        // EPA AQI breakpoints for PM2.5 (gruba aproksimacija)
        if (pm <= 12.0) return (int)(pm / 12.0 * 50);
        if (pm <= 35.4) return (int)(51 + (pm - 12.1) / (35.4 - 12.1) * 49);
        if (pm <= 55.4) return (int)(101 + (pm - 35.5) / (55.4 - 35.5) * 49);
        if (pm <= 150.4) return (int)(151 + (pm - 55.5) / (150.4 - 55.5) * 49);
        if (pm <= 250.4) return (int)(201 + (pm - 150.5) / (250.4 - 150.5) * 99);
        return (int)(301 + Math.Min((pm - 250.5) / (350.4 - 250.5) * 99, 99));
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AirQualityRefreshService is stopping");
        await base.StopAsync(stoppingToken);
    }
}