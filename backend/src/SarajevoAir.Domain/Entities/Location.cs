namespace SarajevoAir.Domain.Entities;

public class Location
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal? Lat { get; set; }
    public decimal? Lon { get; set; }
    public string Source { get; set; } = "openaq";
    public string? ExternalId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
    public ICollection<DailyAggregate> DailyAggregates { get; set; } = new List<DailyAggregate>();
}