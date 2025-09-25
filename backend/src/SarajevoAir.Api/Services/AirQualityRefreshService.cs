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
        
        // Čeka da se aplikacija potpuno pokrene
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshAllCityData(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled refresh cycle");
            }

            // Čeka sledeći ciklus
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
    /// Osvježava Sarajevo podatke (live + forecast)
    /// </summary>
    private async Task RefreshSarajevoData(ISarajevoService sarajevoService, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🏙️ Refreshing Sarajevo data");
            
            // Force fresh data from WAQI API
            var liveTask = sarajevoService.GetLiveAsync(forceFresh: true, cancellationToken);
            var forecastTask = sarajevoService.GetForecastAsync(forceFresh: true, cancellationToken);
            
            await Task.WhenAll(liveTask, forecastTask);
            
            var live = await liveTask;
            var forecast = await forecastTask;
            
            _logger.LogInformation("✅ Sarajevo refreshed - AQI: {Aqi}, Forecast Days: {ForecastDays}", 
                live.OverallAqi, forecast.Forecast.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error refreshing Sarajevo data");
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

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AirQualityRefreshService is stopping");
        await base.StopAsync(stoppingToken);
    }
}