using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SarajevoAir.Application.Dtos;
using SarajevoAir.Application.Interfaces;
using SarajevoAir.Domain.Aqi;
using SarajevoAir.Domain.Entities;

namespace SarajevoAir.Application.Services;

public class MeasurementService : IMeasurementService
{
    private readonly IAppDbContext _context;
    private readonly IOpenAqClient _openAqClient;
    private readonly IAqiCalculator _aqiCalculator;
    private readonly ILogger<MeasurementService> _logger;

    // Default coordinates for Sarajevo
    private const double SarajevoLat = 43.8563;
    private const double SarajevoLon = 18.4131;
    private const int DefaultRadiusKm = 25; // Updated to match API limit

    public MeasurementService(
        IAppDbContext context,
        IOpenAqClient openAqClient,
        IAqiCalculator aqiCalculator,
        ILogger<MeasurementService> logger)
    {
        _context = context;
        _openAqClient = openAqClient;
        _aqiCalculator = aqiCalculator;
        _logger = logger;
    }

    public async Task<LiveAirQualityDto?> GetLiveDataAsync(string city, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find the most recent measurement from the most central location
            var location = await _context.Locations
                .OrderBy(l => l.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (location == null)
            {
                _logger.LogWarning("No locations found for city {City}", city);
                return null;
            }

            var measurement = await _context.Measurements
                .Where(m => m.LocationId == location.Id)
                .OrderByDescending(m => m.TimestampUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (measurement == null)
            {
                _logger.LogWarning("No measurements found for location {LocationId}", location.Id);
                return null;
            }

            var aqiResult = _aqiCalculator.Compute(
                measurement.Pm25,
                measurement.Pm10,
                measurement.O3,
                measurement.No2,
                measurement.So2,
                measurement.Co
            );

            return new LiveAirQualityDto(
                location.Name,
                measurement.TimestampUtc,
                aqiResult.Aqi,
                aqiResult.Category.GetDisplayName(),
                aqiResult.Category.GetHexColor(),
                new PollutantValues(
                    measurement.Pm25,
                    measurement.Pm10,
                    measurement.O3,
                    measurement.No2,
                    measurement.So2,
                    measurement.Co
                ),
                aqiResult.Category.GetRecommendation("bs")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get live data for city {City}", city);
            return null;
        }
    }

    public async Task<HistoryResponseDto> GetHistoryDataAsync(string city, int days, string resolution, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var data = new List<HistoryDataPoint>();

            if (resolution.Equals("day", StringComparison.OrdinalIgnoreCase))
            {
                // Use daily aggregates
                var aggregates = await _context.DailyAggregates
                    .Include(d => d.Location)
                    .Where(d => d.Date >= DateOnly.FromDateTime(cutoffDate))
                    .OrderByDescending(d => d.Date)
                    .Take(days)
                    .ToListAsync(cancellationToken);

                data = aggregates.Select(a => new HistoryDataPoint(
                    a.Date.ToDateTime(TimeOnly.MinValue),
                    a.MaxAqi,
                    a.MaxAqi.HasValue ? CategoryFromAqi(a.MaxAqi.Value).GetDisplayName() : null,
                    new PollutantValues(
                        a.AvgPm25,
                        a.AvgPm10,
                        a.AvgO3,
                        a.AvgNo2,
                        a.AvgSo2,
                        a.AvgCo
                    )
                )).ToList();
            }
            else
            {
                // Use hourly measurements
                var measurements = await _context.Measurements
                    .Include(m => m.Location)
                    .Where(m => m.TimestampUtc >= cutoffDate)
                    .OrderByDescending(m => m.TimestampUtc)
                    .Take(days * 24) // Approximate hourly data
                    .ToListAsync(cancellationToken);

                data = measurements.Select(m => new HistoryDataPoint(
                    m.TimestampUtc,
                    m.ComputedAqi,
                    m.AqiCategory,
                    new PollutantValues(
                        m.Pm25,
                        m.Pm10,
                        m.O3,
                        m.No2,
                        m.So2,
                        m.Co
                    )
                )).ToList();
            }

            return new HistoryResponseDto(city, resolution, days, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get history data for city {City}", city);
            return new HistoryResponseDto(city, resolution, days, new List<HistoryDataPoint>());
        }
    }

    public async Task<CompareResponseDto> GetCompareDataAsync(string[] cities, CancellationToken cancellationToken = default)
    {
        try
        {
            var comparisons = new List<CityComparisonDto>();
            var last24Hours = DateTime.UtcNow.AddHours(-24);

            foreach (var city in cities)
            {
                var liveData = await GetLiveDataAsync(city, cancellationToken);
                
                // Get last 24 hours of data
                var hourlyData = await _context.Measurements
                    .Include(m => m.Location)
                    .Where(m => m.TimestampUtc >= last24Hours && 
                               m.Location!.Name.Contains(city)) // Simple city matching
                    .OrderByDescending(m => m.TimestampUtc)
                    .Take(24)
                    .Select(m => new HistoryDataPoint(
                        m.TimestampUtc,
                        m.ComputedAqi,
                        m.AqiCategory,
                        new PollutantValues(m.Pm25, m.Pm10, m.O3, m.No2, m.So2, m.Co)
                    ))
                    .ToListAsync(cancellationToken);

                comparisons.Add(new CityComparisonDto(city, liveData, hourlyData));
            }

            return new CompareResponseDto(DateTime.UtcNow, comparisons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get compare data for cities {Cities}", string.Join(", ", cities));
            return new CompareResponseDto(DateTime.UtcNow, new List<CityComparisonDto>());
        }
    }

    public async Task<List<LocationInfoDto>> GetLocationsAsync(string city, CancellationToken cancellationToken = default)
    {
        try
        {
            var locations = await _context.Locations
                .Select(l => new LocationInfoDto(
                    l.Id,
                    l.Name,
                    l.Lat,
                    l.Lon,
                    l.Measurements.OrderByDescending(m => m.TimestampUtc)
                        .Select(m => m.TimestampUtc)
                        .FirstOrDefault()
                ))
                .ToListAsync(cancellationToken);

            return locations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get locations for city {City}", city);
            return new List<LocationInfoDto>();
        }
    }

    public async Task FetchAndStoreAsync(string city, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting data fetch for city {City}", city);

            // Fetch locations from OpenAQ
            var locations = await _openAqClient.GetLocationsAsync(
                SarajevoLat, SarajevoLon, DefaultRadiusKm, cancellationToken);

            foreach (var locationDto in locations.Take(5)) // Limit to 5 locations for demo
            {
                var location = await EnsureLocationExistsAsync(locationDto, cancellationToken);
                
                // Fetch sensors for this location
                var sensors = await _openAqClient.GetSensorsForLocationAsync(
                    locationDto.Id, cancellationToken);

                foreach (var sensor in sensors.Take(10)) // Limit sensors per location
                {
                    // Fetch recent measurements
                    var since = DateTime.UtcNow.AddHours(-6); // Last 6 hours
                    var measurements = await _openAqClient.GetMeasurementsForSensorAsync(
                        sensor.Id, since, 100, cancellationToken);

                    await StoreMeasurementsAsync(location.Id, measurements, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Completed data fetch for city {City}", city);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch and store data for city {City}", city);
        }
    }

    public async Task GenerateDailyAggregatesAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        try
        {
            var startOfDay = date.ToDateTime(TimeOnly.MinValue);
            var endOfDay = date.ToDateTime(TimeOnly.MaxValue);

            var locations = await _context.Locations.ToListAsync(cancellationToken);

            foreach (var location in locations)
            {
                var measurements = await _context.Measurements
                    .Where(m => m.LocationId == location.Id && 
                               m.TimestampUtc >= startOfDay && 
                               m.TimestampUtc <= endOfDay)
                    .ToListAsync(cancellationToken);

                if (!measurements.Any()) continue;

                var existingAggregate = await _context.DailyAggregates
                    .FirstOrDefaultAsync(d => d.LocationId == location.Id && d.Date == date, cancellationToken);

                if (existingAggregate == null)
                {
                    var aggregate = new DailyAggregate
                    {
                        LocationId = location.Id,
                        Date = date,
                        AvgPm25 = (decimal?)measurements.Where(m => m.Pm25.HasValue).Average(m => m.Pm25),
                        MaxPm25 = (decimal?)measurements.Where(m => m.Pm25.HasValue).Max(m => m.Pm25),
                        MinPm25 = (decimal?)measurements.Where(m => m.Pm25.HasValue).Min(m => m.Pm25),
                        AvgPm10 = (decimal?)measurements.Where(m => m.Pm10.HasValue).Average(m => m.Pm10),
                        MaxPm10 = (decimal?)measurements.Where(m => m.Pm10.HasValue).Max(m => m.Pm10),
                        MinPm10 = (decimal?)measurements.Where(m => m.Pm10.HasValue).Min(m => m.Pm10),
                        AvgAqi = (int?)measurements.Where(m => m.ComputedAqi.HasValue).Average(m => m.ComputedAqi),
                        MaxAqi = (int?)measurements.Where(m => m.ComputedAqi.HasValue).Max(m => m.ComputedAqi),
                        MinAqi = (int?)measurements.Where(m => m.ComputedAqi.HasValue).Min(m => m.ComputedAqi)
                    };

                    _context.DailyAggregates.Add(aggregate);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Generated daily aggregates for date {Date}", date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate daily aggregates for date {Date}", date);
        }
    }

    private async Task<Location> EnsureLocationExistsAsync(LocationDto locationDto, CancellationToken cancellationToken)
    {
        var existing = await _context.Locations
            .FirstOrDefaultAsync(l => l.ExternalId == locationDto.Id, cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        var location = new Location
        {
            Name = locationDto.Name,
            Lat = locationDto.Latitude.HasValue ? (decimal)locationDto.Latitude.Value : null,
            Lon = locationDto.Longitude.HasValue ? (decimal)locationDto.Longitude.Value : null,
            ExternalId = locationDto.Id,
            Source = "openaq"
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);
        return location;
    }

    private async Task StoreMeasurementsAsync(Guid locationId, List<MeasurementDto> measurements, CancellationToken cancellationToken)
    {
        var groupedByTimestamp = measurements
            .GroupBy(m => m.DatetimeUtc)
            .ToList();

        foreach (var group in groupedByTimestamp)
        {
            var timestamp = group.Key;
            
            // Check if measurement already exists
            var existing = await _context.Measurements
                .FirstOrDefaultAsync(m => m.LocationId == locationId && m.TimestampUtc == timestamp, cancellationToken);

            if (existing != null) continue; // Skip duplicates

            var measurement = new Measurement
            {
                LocationId = locationId,
                TimestampUtc = timestamp
            };

            // Map pollutant values
            foreach (var measurementDto in group)
            {
                switch (measurementDto.Parameter.ToLowerInvariant())
                {
                    case "pm25":
                        measurement.Pm25 = (decimal)measurementDto.Value;
                        break;
                    case "pm10":
                        measurement.Pm10 = (decimal)measurementDto.Value;
                        break;
                    case "o3":
                        measurement.O3 = (decimal)measurementDto.Value;
                        break;
                    case "no2":
                        measurement.No2 = (decimal)measurementDto.Value;
                        break;
                    case "so2":
                        measurement.So2 = (decimal)measurementDto.Value;
                        break;
                    case "co":
                        measurement.Co = (decimal)measurementDto.Value;
                        break;
                }
            }

            // Calculate AQI
            var aqiResult = _aqiCalculator.Compute(
                measurement.Pm25,
                measurement.Pm10,
                measurement.O3,
                measurement.No2,
                measurement.So2,
                measurement.Co
            );

            measurement.ComputedAqi = aqiResult.Aqi;
            measurement.AqiCategory = aqiResult.Category.GetDisplayName();

            _context.Measurements.Add(measurement);
        }
    }

    private static AqiCategory CategoryFromAqi(int aqi) => aqi switch
    {
        <= 50 => AqiCategory.Good,
        <= 100 => AqiCategory.Moderate,
        <= 150 => AqiCategory.USG,
        <= 200 => AqiCategory.Unhealthy,
        <= 300 => AqiCategory.VeryUnhealthy,
        _ => AqiCategory.Hazardous
    };
}