namespace BosniaAir.Api.Dtos;

/// <summary>
/// Represents the root response structure from the WAQI API.
/// Contains the API call status and the main air quality data.
/// </summary>
public record WaqiApiResponse(
    string Status,
    WaqiData? Data
);

/// <summary>
/// Contains the main air quality data from the WAQI API response.
/// Includes AQI value, location information, pollutant measurements, and forecast data.
/// </summary>
public record WaqiData(
    int Aqi,
    int Idx,
    WaqiCity City,
    string? Dominentpol,
    WaqiIaqi? Iaqi,
    WaqiTime Time,
    WaqiForecast? Forecast
);

/// <summary>
/// Represents geographical and identification information for a WAQI monitoring station.
/// Contains coordinates, station name, and URL for more information.
/// </summary>
public record WaqiCity(
    double[] Geo,
    string Name,
    string Url
);

/// <summary>
/// Contains individual air quality index (IAQI) measurements for various pollutants.
/// Each property represents a different pollutant with its measured value.
/// </summary>
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

/// <summary>
/// Represents a single pollutant measurement value from the WAQI API.
/// Contains the measured value for a specific pollutant.
/// </summary>
public record WaqiMeasurement(
    double V
);

/// <summary>
/// Contains timestamp information for when the air quality data was recorded.
/// Includes both human-readable and Unix timestamp formats.
/// </summary>
public record WaqiTime(
    string S,
    string Tz,
    long V,
    string Iso
);

/// <summary>
/// Contains forecast data organized by day for various pollutants.
/// Provides daily air quality predictions.
/// </summary>
public record WaqiForecast(
    WaqiDailyForecast Daily
);

/// <summary>
/// Contains daily forecast entries for different pollutants.
/// Each pollutant type has an array of forecast entries with min/max/average values.
/// </summary>
public record WaqiDailyForecast(
    WaqiForecastEntry[]? O3,
    WaqiForecastEntry[]? Pm10,
    WaqiForecastEntry[]? Pm25,
    WaqiForecastEntry[]? Uvi
);

/// <summary>
/// Represents a single forecast entry for a pollutant on a specific day.
/// Contains the average, minimum, and maximum predicted values.
/// </summary>
public record WaqiForecastEntry(
    double Avg,
    string Day,
    double Max,
    double Min
);