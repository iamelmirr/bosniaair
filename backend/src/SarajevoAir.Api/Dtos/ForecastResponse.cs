namespace SarajevoAir.Api.Dtos;

public record PollutantRangeDto(int Avg, int Min, int Max);

public record ForecastDayDto(
    string Date,
    int Aqi,
    string Category,
    string Color,
    ForecastDayPollutants Pollutants
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
