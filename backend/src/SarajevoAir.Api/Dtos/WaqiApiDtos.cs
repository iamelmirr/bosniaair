/*
===========================================================================================
                               WAQI API DTOs - WORLD AIR QUALITY INDEX API
===========================================================================================

PURPOSE: Data Transfer Objects za parsiranje odgovora od WAQI API
API Documentation: https://aqicn.org/json-api/doc/

WAQI API Response Format:
{
  "status": "ok",
  "data": {
    "aqi": 85,
    "idx": 9267,
    "attributions": [...],
    "city": {
      "geo": [43.8486, 18.3564],
      "name": "Sarajevo, Novo Sarajevo, Bosnia and Herzegovina",
      "url": "https://aqicn.org/city/bosnia-and-herzegovina/sarajevo/novo-sarajevo"
    },
    "dominentpol": "pm25",
    "iaqi": {
      "co": {"v": 3.4},
      "h": {"v": 60.2},
      "no2": {"v": 20.3},
      "o3": {"v": 45.7},
      "p": {"v": 1005.2},
      "pm10": {"v": 40},
      "pm25": {"v": 85},
      "so2": {"v": 8.1},
      "t": {"v": 15.3},
      "w": {"v": 2.5},
      "wg": {"v": 5.2}
    },
    "time": {
      "s": "2024-03-15 14:00:00",
      "tz": "+01:00",
      "v": 1710514800,
      "iso": "2024-03-15T14:00:00+01:00"
    },
    "forecast": {
      "daily": {
        "o3": [
          {"avg": 25, "day": "2024-03-15", "max": 40, "min": 15},
          {"avg": 30, "day": "2024-03-16", "max": 45, "min": 20}
        ],
        "pm10": [...],
        "pm25": [...],
        "uvi": [...]
      }
    }
  }
}
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