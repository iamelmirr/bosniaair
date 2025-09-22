namespace SarajevoAir.Application.Dtos;

// OpenAQ DTOs
public record LocationDto(
    string Id,
    string Name,
    double? Latitude,
    double? Longitude,
    string Country,
    string? City
);

public record SensorDto(
    long Id,
    string Name,
    string LocationId,
    string Parameter,
    string Unit
);

public record MeasurementDto(
    long SensorId,
    DateTime DatetimeUtc,
    double Value,
    string Parameter,
    string Unit
);

// API Response DTOs
public record LiveAirQualityDto(
    string Location,
    DateTime Timestamp,
    int Aqi,
    string Category,
    string CategoryColor,
    PollutantValues Pollutants,
    string Recommendation
);

public record PollutantValues(
    decimal? Pm25,
    decimal? Pm10,
    decimal? O3,
    decimal? No2,
    decimal? So2,
    decimal? Co
);

public record HistoryResponseDto(
    string City,
    string Resolution,
    int Days,
    List<HistoryDataPoint> Data
);

public record HistoryDataPoint(
    DateTime Timestamp,
    int? Aqi,
    string? Category,
    PollutantValues Pollutants
);

public record CompareResponseDto(
    DateTime GeneratedAt,
    List<CityComparisonDto> Cities
);

public record CityComparisonDto(
    string City,
    LiveAirQualityDto? Current,
    List<HistoryDataPoint> Last24Hours
);

public record LocationInfoDto(
    Guid Id,
    string Name,
    decimal? Latitude,
    decimal? Longitude,
    DateTime LastMeasurement
);

public record ShareRequestDto(
    string? City = null,
    int? Aqi = null,
    string? Category = null
);

public record ShareResponseDto(
    string Title,
    string Text,
    string Url
);

public record GroupRecommendationDto(
    string GroupName,
    string Title,
    string Description,
    List<CategoryRecommendation> Recommendations
);

public record CategoryRecommendation(
    string Category,
    string Message,
    string Severity
);