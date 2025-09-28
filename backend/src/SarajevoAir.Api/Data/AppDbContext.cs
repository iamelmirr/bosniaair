using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SarajevoAir.Api.Entities;
using SarajevoAir.Api.Enums;

namespace SarajevoAir.Api.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AirQualityRecord> AirQualityRecords => Set<AirQualityRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      var cityConverter = new EnumToStringConverter<City>();
      var recordTypeConverter = new EnumToStringConverter<AirQualityRecordType>();

      modelBuilder.Entity<AirQualityRecord>(entity =>
      {
        entity.Property(e => e.City).HasConversion(cityConverter).HasMaxLength(32);
        entity.Property(e => e.RecordType).HasConversion(recordTypeConverter).HasMaxLength(16);
        entity.Property(e => e.DominantPollutant).HasMaxLength(32);

    entity.HasIndex(e => new { e.City, e.RecordType, e.Timestamp })
      .HasDatabaseName("IX_AirQuality_CityTypeTimestamp");

    entity.HasIndex(e => new { e.City, e.RecordType })
      .HasFilter($"[{nameof(AirQualityRecord.RecordType)}] = 'Forecast'")
      .IsUnique()
      .HasDatabaseName("UX_AirQuality_ForecastPerCity");
      });
    }
}
