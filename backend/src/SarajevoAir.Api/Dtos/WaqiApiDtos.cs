/*
WAQI API DTOs - World Air Quality Index API
PURPOSE: Data Transfer Objects za parsiranje odgovora od WAQI API
API Documentation: https://aqicn.org/json-api/doc/
*/

namespace SarajevoAir.Api.Dtos;

// Root response object sa WAQI API
public record WaqiApiResponse(
    string Status,
    WaqiData? Data
);

// Main data object
public record WaqiData(
    int Aqi,
    int Idx,
    WaqiCity City,
    string? Dominentpol,
    WaqiIaqi? Iaqi,
    WaqiTime Time,
    WaqiForecast? Forecast
);

// City information
public record WaqiCity(
    double[] Geo,
    string Name,
    string Url
);

// Individual Air Quality Index measurements
public record WaqiIaqi(
    WaqiMeasurement? Co,
    WaqiMeasurement? H,
    WaqiMeasurement? No2,
    WaqiMeasurement? O3,
    WaqiMeasurement? P,
    WaqiMeasurement? Pm10,
    WaqiMeasurement? Pm25,
    WaqiMeasurement? So2,
    WaqiMeasurement? T,
    WaqiMeasurement? W,
    WaqiMeasurement? Wg
);

// Single measurement value
public record WaqiMeasurement(
    double V
);

// Time information
public record WaqiTime(
    string S,
    string Tz,
    long V,
    string Iso
);

// Forecast data
public record WaqiForecast(
    WaqiDailyForecast Daily
);

public record WaqiDailyForecast(
    WaqiForecastEntry[]? O3,
    WaqiForecastEntry[]? Pm10,
    WaqiForecastEntry[]? Pm25,
    WaqiForecastEntry[]? Uvi
);

public record WaqiForecastEntry(
    double Avg,
    string Day,
    double Max,
    double Min
);