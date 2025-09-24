using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SarajevoAir.Application.Interfaces;
using SarajevoAir.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SarajevoAir.Worker;

// Simple models for AQICN API response
public class SimpleAqicnResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public SimpleAqicnData? Data { get; set; }
}

public class SimpleAqicnData
{
    [JsonPropertyName("aqi")]
    public int Aqi { get; set; }
    
    [JsonPropertyName("iaqi")]
    public Dictionary<string, SimpleAqicnMeasurement>? Iaqi { get; set; }
}

public class SimpleAqicnMeasurement
{
    [JsonPropertyName("v")]
    public double V { get; set; }
}

public class AqiBackgroundService : BackgroundService
{
    private readonly ILogger<AqiBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _fetchInterval;
    private readonly HttpClient _httpClient;
    private readonly string _aqicnApiUrl;
    private readonly string _aqicnApiToken;

    public AqiBackgroundService(
        ILogger<AqiBackgroundService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _httpClient = new HttpClient();
        
        _fetchInterval = TimeSpan.FromMinutes(
            int.Parse(configuration["Worker:FetchIntervalMinutes"] ?? "10"));
            
        _aqicnApiUrl = configuration["Aqicn:ApiUrl"] ?? "https://api.waqi.info";
        _aqicnApiToken = configuration["Aqicn:ApiToken"] ?? "";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AqiBackgroundService started. Fetch interval: {FetchInterval}", _fetchInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchAndStoreAqiDataAsync(stoppingToken);
                
                _logger.LogInformation("Next AQI data fetch scheduled in {Interval} minutes", _fetchInterval.TotalMinutes);
                await Task.Delay(_fetchInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("AqiBackgroundService cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AqiBackgroundService cycle");
                // Wait a shorter time on error before retrying
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        
        _logger.LogInformation("AqiBackgroundService stopped");
    }

    private async Task FetchAndStoreAqiDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        try
        {
            _logger.LogInformation("Fetching AQI data from AQICN API for Sarajevo");

            // Fetch data from AQICN API using HttpClient
            var url = $"{_aqicnApiUrl}/feed/sarajevo/?token={_aqicnApiToken}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var aqiData = JsonSerializer.Deserialize<SimpleAqicnResponse>(jsonContent, options);
            
            if (aqiData?.Data == null || aqiData.Status != "ok")
            {
                _logger.LogWarning("No AQI data received from AQICN API or status not ok");
                return;
            }

            // Create simple AQI record - just timestamp and AQI value
            var aqiRecord = new SimpleAqiRecord
            {
                Timestamp = DateTime.UtcNow,
                AqiValue = aqiData.Data.Aqi,
                City = "Sarajevo"
            };

            // Add to database
            dbContext.SimpleAqiRecords.Add(aqiRecord);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully stored AQI record for Sarajevo. AQI: {Aqi} at {Timestamp}", 
                aqiRecord.AqiValue, aqiRecord.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and storing AQI data for Sarajevo");
            throw;
        }
    }

    private async Task<Location> GetOrCreateLocationAsync(IAppDbContext dbContext)
    {
        var externalId = "sarajevo-aqicn";
        var existingLocation = dbContext.Locations
            .FirstOrDefault(l => l.ExternalId == externalId);

        if (existingLocation != null)
        {
            return existingLocation;
        }

        // Create new location
        var newLocation = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Sarajevo",
            ExternalId = externalId,
            Source = "aqicn",
            Lat = 43.8476m, // Approximate coordinates for Sarajevo
            Lon = 18.3564m,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Locations.Add(newLocation);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Created new location for Sarajevo with ID: {LocationId}", newLocation.Id);
        return newLocation;
    }

    private decimal? GetPollutantValue(Dictionary<string, SimpleAqicnMeasurement>? iaqi, string pollutant)
    {
        try
        {
            if (iaqi == null || !iaqi.ContainsKey(pollutant)) return null;
            
            return (decimal)iaqi[pollutant].V;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract {Pollutant} value from IAQI data", pollutant);
        }
        
        return null;
    }

    private string GetAqiCategory(int aqi)
    {
        return aqi switch
        {
            <= 50 => "Good",
            <= 100 => "Moderate",
            <= 150 => "Unhealthy for Sensitive Groups",
            <= 200 => "Unhealthy",
            <= 300 => "Very Unhealthy",
            _ => "Hazardous"
        };
    }

    public override void Dispose()
    {
        _httpClient?.Dispose();
        base.Dispose();
    }
}