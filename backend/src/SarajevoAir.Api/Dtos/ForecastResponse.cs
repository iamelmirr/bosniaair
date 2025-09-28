namespace SarajevoAir.Api.Dtos;

/// <summary>
/// Range of values for a pollutant (average, minimum, maximum)
/// </summary>
public record PollutantRangeDto(int Avg, int Min, int Max);

/// <summary>
/// Daily forecast data including AQI and pollutant ranges
/// </summary>
public record ForecastDayDto(
    string Date = "",
    int Aqi = 0,
    string Category = "",
    string Color = "",
    ForecastDayPollutants? Pollutants = null
);

/// <summary>
/// Pollutant concentration ranges for a forecast day
/// </summary>
public record ForecastDayPollutants(
    PollutantRangeDto? Pm25,
    PollutantRangeDto? Pm10,
    PollutantRangeDto? O3
);

/// <summary>
/// Response containing air quality forecast data for a city
/// </summary>
public record ForecastResponse(
    string City,
    IReadOnlyList<ForecastDayDto> Forecast,
    DateTime Timestamp
);
