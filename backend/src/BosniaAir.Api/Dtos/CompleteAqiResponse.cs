namespace BosniaAir.Api.Dtos;

/// <summary>
/// Complete air quality response combining live data and forecast information
/// </summary>
public record CompleteAqiResponse(
    LiveAqiResponse LiveData,
    ForecastResponse ForecastData,
    DateTime RetrievedAt
);
