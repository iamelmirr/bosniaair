using Microsoft.EntityFrameworkCore;
using BosniaAir.Api.Data;
using BosniaAir.Api.Entities;
using BosniaAir.Api.Enums;
using BosniaAir.Api.Utilities;

namespace BosniaAir.Api.Repositories;

/// <summary>
/// Interface for air quality data repository operations.
/// Provides methods for storing and retrieving cached air quality records.
/// </summary>
public interface IAirQualityRepository
{
    /// <summary>
    /// Adds a new live air quality snapshot to the database.
    /// </summary>
    Task AddLiveSnapshot(AirQualityRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent live snapshot for a city.
    /// </summary>
    Task<AirQualityRecord?> GetLiveData(City city, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates forecast data for a city.
    /// </summary>
    Task UpdateForecast(City city, string forecastJson, DateTime timestamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves forecast data for a city.
    /// </summary>
    Task<AirQualityRecord?> GetForecast(City city, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository implementation for air quality data persistence.
/// Handles caching of live snapshots and forecast data in the database.
/// </summary>
public class AirQualityRepository : IAirQualityRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of AirQualityRepository.
    /// </summary>
    /// <param name="context">Database context for data operations</param>
    public AirQualityRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a new live air quality snapshot to the database.
    /// Sets the record type and creation timestamp automatically.
    /// </summary>
    /// <param name="record">The air quality record to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddLiveSnapshot(AirQualityRecord record, CancellationToken cancellationToken = default)
    {
        record.RecordType = AirQualityRecordType.LiveSnapshot;
        record.CreatedAt = TimeZoneHelper.GetSarajevoTime();
        _context.AirQualityRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves the most recent live snapshot for a city.
    /// Returns null if no data is available.
    /// </summary>
    /// <param name="city">The city to get data for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<AirQualityRecord?> GetLiveData(City city, CancellationToken cancellationToken = default)
    {
        return await _context.AirQualityRecords
            .AsNoTracking()
            .Where(r => r.City == city && r.RecordType == AirQualityRecordType.LiveSnapshot)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts or updates forecast data for a city.
    /// If forecast data already exists, it updates the existing record.
    /// </summary>
    /// <param name="city">The city for the forecast</param>
    /// <param name="forecastJson">JSON string containing forecast data</param>
    /// <param name="timestamp">Timestamp of the forecast</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UpdateForecast(City city, string forecastJson, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        var existing = await _context.AirQualityRecords
            .FirstOrDefaultAsync(r => r.City == city && r.RecordType == AirQualityRecordType.Forecast, cancellationToken);

        if (existing is null)
        {
            _context.AirQualityRecords.Add(new AirQualityRecord
            {
                City = city,
                StationId = city.ToStationId(),
                RecordType = AirQualityRecordType.Forecast,
                Timestamp = timestamp,
                ForecastJson = forecastJson,
                CreatedAt = TimeZoneHelper.GetSarajevoTime()
            });
        }
        else
        {
            existing.Timestamp = timestamp;
            existing.ForecastJson = forecastJson;
            existing.UpdatedAt = TimeZoneHelper.GetSarajevoTime();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves forecast data for a city.
    /// Returns null if no forecast data is available.
    /// </summary>
    /// <param name="city">The city to get forecast for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<AirQualityRecord?> GetForecast(City city, CancellationToken cancellationToken = default)
    {
        return await _context.AirQualityRecords
            .AsNoTracking()
            .Where(r => r.City == city && r.RecordType == AirQualityRecordType.Forecast)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
