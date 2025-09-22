using System.ComponentModel.DataAnnotations.Schema;

namespace SarajevoAir.Domain.Entities;

public class DailyAggregate
{
    public long Id { get; set; }
    public Guid LocationId { get; set; }
    public Location? Location { get; set; }
    public DateOnly Date { get; set; }
    
    // PM2.5 aggregates
    [Column(TypeName = "decimal(8,3)")]
    public decimal? AvgPm25 { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MaxPm25 { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MinPm25 { get; set; }
    
    // PM10 aggregates  
    [Column(TypeName = "decimal(8,3)")]
    public decimal? AvgPm10 { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MaxPm10 { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MinPm10 { get; set; }
    
    // O3 aggregates
    [Column(TypeName = "decimal(8,3)")]
    public decimal? AvgO3 { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MaxO3 { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MinO3 { get; set; }
    
    // NO2 aggregates
    [Column(TypeName = "decimal(8,3)")]
    public decimal? AvgNo2 { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MaxNo2 { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MinNo2 { get; set; }
    
    // SO2 aggregates
    [Column(TypeName = "decimal(8,3)")]
    public decimal? AvgSo2 { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MaxSo2 { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MinSo2 { get; set; }
    
    // CO aggregates
    [Column(TypeName = "decimal(8,3)")]
    public decimal? AvgCo { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MaxCo { get; set; }
    [Column(TypeName = "decimal(8,3)")]
    public decimal? MinCo { get; set; }
    
    // AQI aggregates
    [Column(TypeName = "decimal(5,1)")]
    public decimal? AvgAqi { get; set; }
    public int? MaxAqi { get; set; }
    public int? MinAqi { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}