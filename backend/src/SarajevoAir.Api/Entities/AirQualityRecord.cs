using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SarajevoAir.Api.Enums;
using SarajevoAir.Api.Utilities;

namespace SarajevoAir.Api.Entities;

/// <summary>
/// Unified entity that stores both live AQI snapshots (historical) and the latest forecast per city.
/// </summary>
public class AirQualityRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public City City { get; set; }

    [Required]
    public AirQualityRecordType RecordType { get; set; }

    /// <summary>
    /// Station identifier used for WAQI API calls (e.g. @10557).
    /// </summary>
    [MaxLength(16)]
    public string StationId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when data was collected (local Sarajevo time for convenience).
    /// </summary>
    public DateTime Timestamp { get; set; }

    // Live snapshot specific fields (kept for history)
    public int? AqiValue { get; set; }
    public string? DominantPollutant { get; set; }
    public double? Pm25 { get; set; }
    public double? Pm10 { get; set; }
    public double? O3 { get; set; }
    public double? No2 { get; set; }
    public double? Co { get; set; }
    public double? So2 { get; set; }

    /// <summary>
    /// JSON payload (serialized) storing daily forecast array. Only populated for forecast records.
    /// </summary>
    public string? ForecastJson { get; set; }

    public DateTime CreatedAt { get; set; } = TimeZoneHelper.GetSarajevoTime();
    public DateTime? UpdatedAt { get; set; }
}
