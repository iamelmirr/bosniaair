namespace SarajevoAir.Api.Dtos;

public record PollutantRangeDto(int Avg, int Min, int Max);

public record ForecastDayDto(
    string Date = "",
    int Aqi = 0,
    string Category = "",
    string Color = "",
    ForecastDayPollutants? Pollutants = null
);

public record ForecastDayPollutants(
    PollutantRangeDto? Pm25,
    PollutantRangeDto? Pm10,
    PollutantRangeDto? O3
);

public record ForecastResponse(
    string City,
    IReadOnlyList<ForecastDayDto> Forecast,
    DateTime Timestamp
);
