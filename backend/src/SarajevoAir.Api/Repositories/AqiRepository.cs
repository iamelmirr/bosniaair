using Microsoft.EntityFrameworkCore;
using SarajevoAir.Api.Data;
using SarajevoAir.Api.Entities;

namespace SarajevoAir.Api.Repositories;

public interface IAqiRepository
{
    Task AddRecordAsync(SimpleAqiRecord record, CancellationToken cancellationToken = default);
    Task<SimpleAqiRecord?> GetMostRecentAsync(string city, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SimpleAqiRecord>> GetRecentAsync(string city, int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SimpleAqiRecord>> GetRangeAsync(string city, DateTime fromUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SimpleAqiRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class AqiRepository : IAqiRepository
{
    private readonly AppDbContext _context;

    public AqiRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddRecordAsync(SimpleAqiRecord record, CancellationToken cancellationToken = default)
    {
        _context.SimpleAqiRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SimpleAqiRecord?> GetMostRecentAsync(string city, CancellationToken cancellationToken = default)
    {
        return await _context.SimpleAqiRecords
            .AsNoTracking()
            .Where(r => r.City == city)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SimpleAqiRecord>> GetRecentAsync(string city, int count, CancellationToken cancellationToken = default)
    {
        return await _context.SimpleAqiRecords
            .AsNoTracking()
            .Where(r => r.City == city)
            .OrderByDescending(r => r.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SimpleAqiRecord>> GetRangeAsync(string city, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        return await _context.SimpleAqiRecords
            .AsNoTracking()
            .Where(r => r.City == city && r.Timestamp >= fromUtc)
            .OrderBy(r => r.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SimpleAqiRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SimpleAqiRecords
            .AsNoTracking()
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
    var record = await _context.SimpleAqiRecords.FindAsync(new object[] { id }, cancellationToken);
        if (record is null)
        {
            return;
        }

        _context.SimpleAqiRecords.Remove(record);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
