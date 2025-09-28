using Microsoft.EntityFrameworkCore;
using SarajevoAir.Api.Data;
using SarajevoAir.Api.Entities;
using SarajevoAir.Api.Enums;
using SarajevoAir.Api.Utilities;

namespace SarajevoAir.Api.Repositories;

public interface IAirQualityRepository
{
    Task AddLiveSnapshotAsync(AirQualityRecord record, CancellationToken cancellationToken = default);
    Task<AirQualityRecord?> GetLatestSnapshotAsync(City city, CancellationToken cancellationToken = default);
    Task UpsertForecastAsync(City city, string forecastJson, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<AirQualityRecord?> GetForecastAsync(City city, CancellationToken cancellationToken = default);
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
        record.CreatedAt = TimeZoneHelper.GetSarajevoTime();
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

    public async Task<AirQualityRecord?> GetForecastAsync(City city, CancellationToken cancellationToken = default)
    {
        return await _context.AirQualityRecords
            .AsNoTracking()
            .Where(r => r.City == city && r.RecordType == AirQualityRecordType.Forecast)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
