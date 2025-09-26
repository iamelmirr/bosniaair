/*
=== SARAJEVO MEASUREMENTS ENTITY ===

HIGH LEVEL OVERVIEW:
SarajevoMeasurement predstavlja single snapshot svih pollutant measurements za Sarajevo
Koristi se za čuvanje PM2.5, PM10, O3, NO2, CO, SO2 vrednosti u određeno vreme

DESIGN DECISIONS:
1. SARAJEVO-SPECIFIC - samo za Sarajevo grad (ne generic kao SimpleAqiRecord)
2. COMPREHENSIVE - sve pollutant types u jednom record-u
3. NULLABLE VALUES - pollutants mogu biti unavailable u WAQI response
4. UTC TIMESTAMPS - konzistentno sa SimpleAqiRecord pristupom
*/

using System.ComponentModel.DataAnnotations;
using SarajevoAir.Api.Utilities;

namespace SarajevoAir.Api.Entities;

/// <summary>
/// Entity za čuvanje detaljnih pollutant measurements za Sarajevo
/// Ova tabela se refreshuje svakih 10 minuta iz WAQI API-ja
/// </summary>
public class SarajevoMeasurement
{
    /// <summary>
    /// Primary key - auto increment
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Vreme kada su measurements zabeleženi (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; } = TimeZoneHelper.GetSarajevoTime();

    /// <summary>
    /// PM2.5 koncentracija (mikrogrami po kubnom metru)
    /// Najvažniji pollutant za health impacts
    /// </summary>
    public double? Pm25 { get; set; }

    /// <summary>
    /// PM10 koncentracija (mikrogrami po kubnom metru)
    /// Coarse particulate matter
    /// </summary>
    public double? Pm10 { get; set; }

    /// <summary>
    /// Ozon O3 koncentracija (mikrogrami po kubnom metru)
    /// Ground-level ozone pollutant
    /// </summary>
    public double? O3 { get; set; }

    /// <summary>
    /// Nitrogen Dioxide NO2 koncentracija (mikrogrami po kubnom metru)
    /// Primarno iz vehicle emissions
    /// </summary>
    public double? No2 { get; set; }

    /// <summary>
    /// Carbon Monoxide CO koncentracija (miligrama po kubnom metru)
    /// Toxic gas iz incomplete combustion
    /// </summary>
    public double? Co { get; set; }

    /// <summary>
    /// Sulfur Dioxide SO2 koncentracija (mikrogrami po kubnom metru)
    /// Primarno iz industrial sources
    /// </summary>
    public double? So2 { get; set; }

    /// <summary>
    /// AQI vrednost već izračunata od strane WAQI API-ja
    /// Ne računa se lokalno - čuva se direktno iz API response-a
    /// </summary>
    public int? AqiValue { get; set; }
}