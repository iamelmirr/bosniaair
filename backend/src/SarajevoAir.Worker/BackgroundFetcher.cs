using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SarajevoAir.Application.Interfaces;

namespace SarajevoAir.Worker;

public class BackgroundFetcher : BackgroundService
{
    private readonly ILogger<BackgroundFetcher> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _fetchInterval;
    private readonly TimeSpan _aggregateInterval;

    public BackgroundFetcher(
        ILogger<BackgroundFetcher> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        _fetchInterval = TimeSpan.FromMinutes(
            int.Parse(configuration["Worker:FetchIntervalMinutes"] ?? "10"));
        _aggregateInterval = TimeSpan.FromHours(
            int.Parse(configuration["Worker:AggregateIntervalHours"] ?? "1"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundFetcher started. Fetch interval: {FetchInterval}, Aggregate interval: {AggregateInterval}",
            _fetchInterval, _aggregateInterval);

        var lastAggregateTime = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Data fetching cycle
                await FetchDataCycleAsync(stoppingToken);

                // Daily aggregation (once per hour)
                if (DateTime.UtcNow - lastAggregateTime >= _aggregateInterval)
                {
                    await GenerateAggregatesAsync(stoppingToken);
                    lastAggregateTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during background fetch cycle");
            }

            // Wait for next cycle
            await Task.Delay(_fetchInterval, stoppingToken);
        }

        _logger.LogInformation("BackgroundFetcher stopped");
    }

    private async Task FetchDataCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var openAqClient = scope.ServiceProvider.GetRequiredService<IOpenAqClient>();
        var measurementService = scope.ServiceProvider.GetRequiredService<IMeasurementService>();

        try
        {
            _logger.LogInformation("Starting data fetch cycle for Sarajevo area");

            // Use the service method that handles the full flow
            await measurementService.FetchAndStoreAsync("Sarajevo", cancellationToken);

            _logger.LogInformation("Successfully completed data fetch and store for Sarajevo");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data fetch cycle for Sarajevo");
        }
    }

    private async Task GenerateAggregatesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var measurementService = scope.ServiceProvider.GetRequiredService<IMeasurementService>();

        try
        {
            _logger.LogInformation("Starting daily aggregates generation");

            // Generate aggregates for yesterday and today
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            await measurementService.GenerateDailyAggregatesAsync(yesterday, cancellationToken);
            await measurementService.GenerateDailyAggregatesAsync(today, cancellationToken);

            _logger.LogInformation("Daily aggregates generation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily aggregates generation failed");
        }
    }
}