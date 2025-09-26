/*
=== FORECAST DATA REQUEST DTO ===
Enkapsulira parametre za forecast endpoint
Demonstrira kako Request DTOs omogućavaju lakše proširivanje funkcionalnosti

BUDUĆE MOGUĆNOSTI:
- Filtriranje po datumima
- Broj dana forecasta
- Granularnost podataka
*/

using System.ComponentModel.DataAnnotations;

namespace SarajevoAir.Api.Dtos.Requests;

/// <summary>
/// Request objekat za dohvatanje forecast podataka
/// </summary>
public class ForecastRequest
{
    /// <summary>
    /// Forsiraj fresh API poziv zaobilazeći cache
    /// </summary>
    public bool ForceFresh { get; set; } = false;

    /// <summary>
    /// Broj dana forecast podataka (1-7)
    /// Default: 5 dana
    /// </summary>
    [Range(1, 7, ErrorMessage = "Days must be between 1 and 7")]
    public int Days { get; set; } = 5;

    /// <summary>
    /// Uključi detaljne pollutant informacije
    /// </summary>
    public bool IncludeDetailedPollutants { get; set; } = true;
}