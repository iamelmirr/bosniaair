using Microsoft.EntityFrameworkCore;
using SarajevoAir.Api.Data;
using SarajevoAir.Api.Entities;
using SarajevoAir.Api.Enums;

namespace SarajevoAir.Api.Repositories;

public interface IAirQualityRepository
{
    Task AddLiveSnapshotAsync(AirQualityRecord record, CancellationToken cancellationToken = default);
    Task<AirQualityRecord?> GetLatestSnapshotAsync(City city, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AirQualityRecord>> GetSnapshotHistoryAsync(City city, int limit, CancellationToken cancellationToken = default);
    Task UpsertForecastAsync(City city, string forecastJson, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<AirQualityRecord?> GetForecastAsync(City city, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<City, AirQualityRecord>> GetLatestSnapshotsAsync(IEnumerable<City> cities, CancellationToken cancellationToken = default);
}

public class AirQualityRepository : IAirQualityRepository
{
    private readonly AppDbContext _context;

    public AirQualityRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddLiveSnapshotAsync(AirQualityRecord record, CancellationToken cancellationToken = default)
    {
        record.RecordType = AirQualityRecordType.LiveSnapshot;
        record.CreatedAt = DateTime.UtcNow;
        _context.AirQualityRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AirQualityRecord?> GetLatestSnapshotAsync(City city, CancellationToken cancellationToken = default)
    {
        return await _context.AirQualityRecords
            .AsNoTracking()
            .Where(r => r.City == city && r.RecordType == AirQualityRecordType.LiveSnapshot)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AirQualityRecord>> GetSnapshotHistoryAsync(City city, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.AirQualityRecords
            .AsNoTracking()
            .Where(r => r.City == city && r.RecordType == AirQualityRecordType.LiveSnapshot)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertForecastAsync(City city, string forecastJson, DateTime timestamp, CancellationToken cancellationToken = default)
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
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Timestamp = timestamp;
            existing.ForecastJson = forecastJson;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AirQualityRecord?> GetForecastAsync(City city, CancellationToken cancellationToken = default)
    {
        return await _context.AirQualityRecords
            .AsNoTracking()
            .Where(r => r.City == city && r.RecordType == AirQualityRecordType.Forecast)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<City, AirQualityRecord>> GetLatestSnapshotsAsync(IEnumerable<City> cities, CancellationToken cancellationToken = default)
    {
        var cityList = cities.ToList();
        var results = await _context.AirQualityRecords
            .AsNoTracking()
            .Where(r => cityList.Contains(r.City) && r.RecordType == AirQualityRecordType.LiveSnapshot)
            .GroupBy(r => r.City)
            .Select(g => g.OrderByDescending(x => x.Timestamp).First())
            .ToListAsync(cancellationToken);

        return results.ToDictionary(r => r.City, r => r);
    }
}
