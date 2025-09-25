using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SarajevoAir.Api.Services;

public class AirQualityRefreshService : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);

    private readonly IAirQualityService _airQualityService;
    private readonly IForecastService _forecastService;
    private readonly ILogger<AirQualityRefreshService> _logger;

    public AirQualityRefreshService(
        IAirQualityService airQualityService,
        IForecastService forecastService,
        ILogger<AirQualityRefreshService> logger)
    {
        _airQualityService = airQualityService;
        _forecastService = forecastService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Air quality refresh service started");

        await RefreshAsync(stoppingToken);

        try
        {
            using var timer = new PeriodicTimer(RefreshInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Air quality refresh service stopping");
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refreshing Sarajevo AQI and forecast data");
            await _airQualityService.GetLiveAqiAsync("Sarajevo", forceFresh: true, cancellationToken);
            await _forecastService.GetForecastAsync("Sarajevo", forceFresh: true, cancellationToken);
            _logger.LogInformation("Refresh completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Sarajevo air quality data");
        }
    }
}
