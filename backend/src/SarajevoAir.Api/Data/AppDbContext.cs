using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SarajevoAir.Api.Entities;
using SarajevoAir.Api.Enums;

namespace SarajevoAir.Api.Data;

/// <summary>
/// Entity Framework Core database context for the SarajevoAir application.
/// Manages the database connection and provides access to air quality data entities.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the DbSet for air quality records, providing access to all air quality data in the database.
    /// </summary>
    public DbSet<AirQualityRecord> AirQualityRecords => Set<AirQualityRecord>();

    /// <summary>
    /// Configures the database model and relationships when the context is created.
    /// Sets up enum conversions, property constraints, and database indexes for optimal performance.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
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
