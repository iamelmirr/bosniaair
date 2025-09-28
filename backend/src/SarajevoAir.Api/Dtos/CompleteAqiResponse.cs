namespace SarajevoAir.Api.Dtos;

public record CompleteAqiResponse(
    LiveAqiResponse LiveData,
    ForecastResponse ForecastData,
    DateTime RetrievedAt
);
