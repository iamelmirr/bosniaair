using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BosniaAir.Api.Enums;
using BosniaAir.Api.Utilities;

namespace BosniaAir.Api.Entities;

/// <summary>
/// Database entity representing cached air quality data.
/// Stores both live measurements and forecast data with timestamps.
/// </summary>
public class AirQualityRecord
{
    /// <summary>
    /// Primary key for the record
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The city this record belongs to
    /// </summary>
    [Required]
    public City City { get; set; }

    /// <summary>
    /// Type of record (Live or Forecast)
    /// </summary>
    [Required]
    public AirQualityRecordType RecordType { get; set; }

    /// <summary>
    /// External station identifier from WAQI API
    /// </summary>
    [MaxLength(16)]
    public string StationId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the data was measured/collected
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Overall Air Quality Index value
    /// </summary>
    public int? AqiValue { get; set; }

    /// <summary>
    /// The pollutant with the highest concentration
    /// </summary>
    public string? DominantPollutant { get; set; }

    /// <summary>
    /// PM2.5 concentration in µg/m³
    /// </summary>
    public double? Pm25 { get; set; }

    /// <summary>
    /// PM10 concentration in µg/m³
    /// </summary>
    public double? Pm10 { get; set; }

    /// <summary>
    /// Ozone concentration in µg/m³
    /// </summary>
    public double? O3 { get; set; }

    /// <summary>
    /// Nitrogen dioxide concentration in µg/m³
    /// </summary>
    public double? No2 { get; set; }

    /// <summary>
    /// Carbon monoxide concentration in µg/m³
    /// </summary>
    public double? Co { get; set; }

    /// <summary>
    /// Sulfur dioxide concentration in µg/m³
    /// </summary>
    public double? So2 { get; set; }

    /// <summary>
    /// JSON string containing forecast data (for forecast records)
    /// </summary>
    public string? ForecastJson { get; set; }

    /// <summary>
    /// When this record was first created (in Sarajevo timezone)
    /// </summary>
    public DateTime CreatedAt { get; set; } = TimeZoneHelper.GetSarajevoTime();

    /// <summary>
    /// When this record was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
