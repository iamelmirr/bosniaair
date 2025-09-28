using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SarajevoAir.Api.Enums;
using SarajevoAir.Api.Utilities;

namespace SarajevoAir.Api.Entities;

public class AirQualityRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public City City { get; set; }

    [Required]
    public AirQualityRecordType RecordType { get; set; }

    [MaxLength(16)]
    public string StationId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public int? AqiValue { get; set; }
    public string? DominantPollutant { get; set; }
    public double? Pm25 { get; set; }
    public double? Pm10 { get; set; }
    public double? O3 { get; set; }
    public double? No2 { get; set; }
    public double? Co { get; set; }
    public double? So2 { get; set; }

    public string? ForecastJson { get; set; }

    public DateTime CreatedAt { get; set; } = TimeZoneHelper.GetSarajevoTime();
    public DateTime? UpdatedAt { get; set; }
}
