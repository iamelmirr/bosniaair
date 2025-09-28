namespace SarajevoAir.Api.Dtos;
public record WaqiApiResponse(
    string Status,
    WaqiData? Data
);
public record WaqiData(
    int Aqi,
    int Idx,
    WaqiCity City,
    string? Dominentpol,
    WaqiIaqi? Iaqi,
    WaqiTime Time,
    WaqiForecast? Forecast
);
public record WaqiCity(
    double[] Geo,
    string Name,
    string Url
);
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
public record WaqiMeasurement(
    double V
);
public record WaqiTime(
    string S,
    string Tz,
    long V,
    string Iso
);
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