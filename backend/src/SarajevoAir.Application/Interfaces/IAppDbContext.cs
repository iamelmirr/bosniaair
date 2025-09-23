using Microsoft.EntityFrameworkCore;
using SarajevoAir.Domain.Entities;

namespace SarajevoAir.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Location> Locations { get; }
    DbSet<Measurement> Measurements { get; }
    DbSet<DailyAggregate> DailyAggregates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}