using Microsoft.EntityFrameworkCore;
using SarajevoAir.Api.Entities;

namespace SarajevoAir.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SimpleAqiRecord> SimpleAqiRecords => Set<SimpleAqiRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SimpleAqiRecord>(entity =>
        {
            entity.HasIndex(e => new { e.City, e.Timestamp })
                  .HasDatabaseName("IX_AqiRecords_CityTimestamp");
        });
    }
}
