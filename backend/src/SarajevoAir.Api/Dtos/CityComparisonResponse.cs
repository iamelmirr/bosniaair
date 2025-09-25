namespace SarajevoAir.Api.Dtos;

public record CityComparisonEntry(
    string City,
    int? Aqi,
    string Category,
    string Color,
    string? DominantPollutant,
    DateTime? Timestamp,
    string? Error = null
);

public record CityComparisonResponse(
    IReadOnlyList<CityComparisonEntry> Cities,
    DateTime ComparedAt,
    int TotalCities
);
