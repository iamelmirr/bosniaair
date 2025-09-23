using System.Text.Json;
using Microsoft.Extensions.Options;
using SarajevoAir.Api.Configuration;
using SarajevoAir.Api.Models;

namespace SarajevoAir.Api.Services;

public interface IAqicnClient
{
    Task<AqicnResponse?> GetCityDataAsync(string? city = null, CancellationToken cancellationToken = default);
}

public class AqicnClient : IAqicnClient
{
    private readonly HttpClient _httpClient;
    private readonly AqicnConfiguration _config;
    private readonly ILogger<AqicnClient> _logger;

    public AqicnClient(HttpClient httpClient, IOptions<AqicnConfiguration> config, ILogger<AqicnClient> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<AqicnResponse?> GetCityDataAsync(string? city = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var targetCity = city ?? _config.City;
            var url = $"{_config.ApiUrl}/feed/{targetCity}/?token={_config.ApiToken}";
            
            _logger.LogInformation("Fetching air quality data from AQICN for city: {City}", targetCity);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<AqicnResponse>(jsonContent, options);
            
            if (result?.Status != "ok")
            {
                _logger.LogWarning("AQICN API returned error status: {Status}", result?.Status);
                return null;
            }
            
            _logger.LogInformation("Successfully fetched air quality data for {City}. AQI: {Aqi}", 
                result?.Data?.City?.Name, result?.Data?.Aqi);
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error when fetching data from AQICN API");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error when processing AQICN response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when fetching data from AQICN API");
            return null;
        }
    }
}