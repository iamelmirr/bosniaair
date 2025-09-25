using System.ComponentModel.DataAnnotations;

namespace SarajevoAir.Api.Entities;

public class SimpleAqiRecord
{
    [Key]
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public int AqiValue { get; set; }

    public string City { get; set; } = "Sarajevo";
}
