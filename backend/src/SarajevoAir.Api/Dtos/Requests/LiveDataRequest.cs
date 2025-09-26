/*
=== LIVE DATA REQUEST DTO ===
Enkapsulira sve parametre za live data endpoint
Pokazuje clean API design umjesto primitive parametara

PREDNOSTI:
- Type safety - sve parametre grupiramo u objekat
- Extensibility - lako dodavamo nove parametre
- Validation - možemo dodati FluentValidation
- Testing - lakše mockiranje i testiranje
*/

using System.ComponentModel.DataAnnotations;

namespace SarajevoAir.Api.Dtos.Requests;

/// <summary>
/// Request objekat za dohvatanje live AQI podataka
/// </summary>
public class LiveDataRequest
{
    /// <summary>
    /// Forsiraj fresh API poziv zaobilazeći cache
    /// Default: false - koristi cache ako je dostupan
    /// </summary>
    [Display(Name = "Force Fresh Data")]
    public bool ForceFresh { get; set; } = false;

    /// <summary>
    /// Timeout za API poziv u sekundama
    /// Default: 30 sekundi
    /// </summary>
    [Range(5, 300, ErrorMessage = "Timeout must be between 5 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;
}