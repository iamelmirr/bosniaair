using System.ComponentModel.DataAnnotations.Schema;

namespace SarajevoAir.Domain.Entities;

public class Measurement
{
    public long Id { get; set; }
    public Guid LocationId { get; set; }
    public Location? Location { get; set; }
    public DateTime TimestampUtc { get; set; }
    
    // Pollutant concentrations
    [Column(TypeName = "decimal(8,3)")]
    public decimal? Pm25 { get; set; }
    
    [Column(TypeName = "decimal(8,3)")]
    public decimal? Pm10 { get; set; }
    
    [Column(TypeName = "decimal(8,3)")]
    public decimal? O3 { get; set; }
    
    [Column(TypeName = "decimal(8,3)")]
    public decimal? No2 { get; set; }
    
    [Column(TypeName = "decimal(8,3)")]
    public decimal? So2 { get; set; }
    
    [Column(TypeName = "decimal(8,3)")]
    public decimal? Co { get; set; }
    
    // Raw JSON from OpenAQ
    [Column(TypeName = "jsonb")]
    public string? RawJson { get; set; }
    
    // Computed AQI values
    public int? ComputedAqi { get; set; }
    public string? AqiCategory { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}