/*
=== SARAJEVO FORECAST ENTITY ===

HIGH LEVEL OVERVIEW:
SarajevoForecast predstavlja daily forecast podatke za Sarajevo
Koristi se za čuvanje prognoze AQI i PM2.5 vrednosti za sledeće dane

DESIGN DECISIONS:
1. DAILY GRANULARITY - jedan record po danu (forecast.daily[] iz WAQI)
2. PM25 FOCUS - min/max/avg PM2.5 kao primary pollutant
3. AQI SUMMARY - general air quality prediction
4. DATE-BASED - koristi Date field umesto precise Timestamp
*/

using System.ComponentModel.DataAnnotations;

namespace SarajevoAir.Api.Entities;

/// <summary>
/// Entity za čuvanje daily forecast podataka za Sarajevo
/// Ova tabela se refreshuje svakih 10 minuta iz WAQI API-ja (forecast.daily)
/// </summary>
public class SarajevoForecast
{
    /// <summary>
    /// Primary key - auto increment
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Datum za koji važi forecast (samo datum bez vremena)
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Predicted AQI vrednost za taj dan
    /// </summary>
    public int? Aqi { get; set; }

    /// <summary>
    /// Minimalna PM2.5 vrednost predviđena za taj dan
    /// </summary>
    public double? Pm25Min { get; set; }

    /// <summary>
    /// Maksimalna PM2.5 vrednost predviđena za taj dan  
    /// </summary>
    public double? Pm25Max { get; set; }

    /// <summary>
    /// Prosečna PM2.5 vrednost predviđena za taj dan
    /// </summary>
    public double? Pm25Avg { get; set; }

    /// <summary>
    /// Vreme kada je forecast kreiran/refreshovan (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}