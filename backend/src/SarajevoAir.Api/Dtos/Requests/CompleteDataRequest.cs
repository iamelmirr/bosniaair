/*
=== COMPLETE DATA REQUEST DTO ===
Kombinira live i forecast parametre
Pokazuje kompoziciju Request objekata

DESIGN PATTERN:
Umjesto miješanja parametara, koristimo kompoziciju
*/

using System.ComponentModel.DataAnnotations;

namespace SarajevoAir.Api.Dtos.Requests;

/// <summary>
/// Request objekat za dohvatanje kompletnih podataka (live + forecast)
/// </summary>
public class CompleteDataRequest
{
    /// <summary>
    /// Paramteri za live podatke
    /// </summary>
    public LiveDataRequest LiveData { get; set; } = new();

    /// <summary>
    /// Parametri za forecast podatke
    /// </summary>
    public ForecastRequest Forecast { get; set; } = new();
}