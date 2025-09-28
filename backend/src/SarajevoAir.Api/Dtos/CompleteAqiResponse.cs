/*
===========================================================================================
                               COMPLETE AQI RESPONSE - COMBINED LIVE + FORECAST
===========================================================================================

PURPOSE: Combined response za live + forecast podatke u jednom API pozivu
Optimizacija za frontend koji treba oba dataseta istovremeno

USED BY: /complete endpoint za efikasniji data fetching
*/

namespace SarajevoAir.Api.Dtos;

/// <summary>
/// Combined response containing both live and forecast air quality data
/// Optimized for frontend applications that need both datasets
/// </summary>
/// <param name="LiveData">Current air quality conditions</param>
/// <param name="ForecastData">Air quality forecast for coming days</param>
/// <param name="RetrievedAt">Timestamp kada su data combined</param>
public record CompleteAqiResponse(
    LiveAqiResponse LiveData,
    ForecastResponse ForecastData,
    DateTime RetrievedAt
);
