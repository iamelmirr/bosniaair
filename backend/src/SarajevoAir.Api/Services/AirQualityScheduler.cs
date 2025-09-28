using SarajevoAir.Api.Enums;

namespace SarajevoAir.Api.Services;

/// <summary>
/// Background service that periodically refreshes air quality data for configured cities.
/// Runs as a hosted service in ASP.NET Core.
/// </summary>
public class AirQualityScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AirQualityScheduler> _logger;
    private readonly TimeSpan _interval;
    private readonly IReadOnlyList<City> _cities;

    /// <summary>
    /// Initializes the scheduler with cities and interval from config.
    /// Defaults to all cities if none specified.
    /// <example>
    /// appsettings.json:
    /// "Worker": { "FetchIntervalMinutes": 15, "Cities": ["Sarajevo", "Tuzla"] }
    /// </example>
    /// </summary>
    public AirQualityScheduler(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AirQualityScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var intervalMinutes = configuration.GetValue("Worker:FetchIntervalMinutes", 10);
        _interval = TimeSpan.FromMinutes(Math.Max(1, intervalMinutes));

        var configuredCities = configuration.GetSection("Worker:Cities").Get<string[]>() ?? Array.Empty<string>();
        _cities = configuredCities.Length > 0
            ? configuredCities.Select(ParseCity).Where(c => c.HasValue).Select(c => c!.Value).Distinct().ToArray()
            : Enum.GetValues<City>();
    }

    /// <summary>
    /// Main background loop: refreshes immediately, then repeats on interval.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cityList = string.Join(", ", _cities.Select(c => c.ToDisplayName()));
        _logger.LogInformation(
            "Air quality scheduler starting with {CityCount} cities ({Cities}). Interval: {Interval} minutes",
            _cities.Count,
            cityList,
            _interval.TotalMinutes);
        await RunRefreshCycle(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await RunRefreshCycle(stoppingToken);
        }

        _logger.LogInformation("Air quality scheduler stopped");
    }

    /// <summary>
    /// Refreshes all configured cities in parallel.
    /// </summary>
    private async Task RunRefreshCycle(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled refresh for {CityCount} cities", _cities.Count);

        var tasks = _cities.Select(city => RefreshCityAsync(city, cancellationToken)).ToList();
        await Task.WhenAll(tasks);

        _logger.LogInformation("Completed scheduled refresh");
    }

    /// <summary>
    /// Refreshes a single city using a scoped service instance. Logs errors but doesn't fail the whole cycle.
    /// </summary>
    private async Task RefreshCityAsync(City city, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var airQualityService = scope.ServiceProvider.GetRequiredService<IAirQualityService>();
            
            await airQualityService.RefreshCityAsync(city, cancellationToken);
            _logger.LogInformation("Refreshed data for {City}", city);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh data for {City}", city);
        }
    }

    /// <summary>
    /// Parses a city name string to City enum, case-insensitive.
    /// </summary>
    private static City? ParseCity(string value)
    {
        return Enum.TryParse<City>(value, true, out var city) ? city : null;
    }
}
