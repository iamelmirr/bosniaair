using Microsoft.EntityFrameworkCore;
using SarajevoAir.Application.Interfaces;
using SarajevoAir.Domain.Entities;

namespace SarajevoAir.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<DailyAggregate> DailyAggregates => Set<DailyAggregate>();
    public DbSet<SimpleAqiRecord> SimpleAqiRecords => Set<SimpleAqiRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Location configuration
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Source)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("openaq");
            entity.Property(e => e.ExternalId)
                .HasMaxLength(100);
            
            entity.HasIndex(e => e.ExternalId);
            entity.HasIndex(e => new { e.Lat, e.Lon });
        });

        // Measurement configuration
        modelBuilder.Entity<Measurement>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Location)
                .WithMany(e => e.Measurements)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.AqiCategory)
                .HasMaxLength(50);

            // Indexes for performance
            entity.HasIndex(e => new { e.LocationId, e.TimestampUtc })
                .HasDatabaseName("IX_Measurements_Location_Timestamp")
                .IsDescending(false, true); // LocationId ASC, TimestampUtc DESC
                
            entity.HasIndex(e => e.TimestampUtc)
                .HasDatabaseName("IX_Measurements_Timestamp");
                
            entity.HasIndex(e => new { e.LocationId, e.ComputedAqi })
                .HasDatabaseName("IX_Measurements_Location_AQI")
                .IsDescending(false, true); // LocationId ASC, ComputedAqi DESC

            // Unique constraint to prevent duplicate measurements
            entity.HasIndex(e => new { e.LocationId, e.TimestampUtc })
                .IsUnique()
                .HasDatabaseName("UX_Measurements_Location_Timestamp");
        });

        // DailyAggregate configuration
        modelBuilder.Entity<DailyAggregate>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Location)
                .WithMany(e => e.DailyAggregates)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.LocationId, e.Date })
                .IsUnique()
                .HasDatabaseName("UX_DailyAggregates_Location_Date");
                
            entity.HasIndex(e => e.Date)
                .HasDatabaseName("IX_DailyAggregates_Date");
        });
    }
}