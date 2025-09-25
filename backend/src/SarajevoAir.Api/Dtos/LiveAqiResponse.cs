namespace SarajevoAir.Api.Dtos;

public record CoordinateDto(double Latitude, double Longitude);

public record MeasurementDto(
    string Id,
    string City,
    string LocationName,
    string Parameter,
    double Value,
    string Unit,
    DateTime Timestamp,
    string SourceName,
    CoordinateDto? Coordinates = null,
    AveragingPeriodDto? AveragingPeriod = null
);

public record AveragingPeriodDto(double Value, string Unit);

public record LiveAqiResponse(
    string City,
    int OverallAqi,
    string AqiCategory,
    string Color,
    string HealthMessage,
    DateTime Timestamp,
    IReadOnlyList<MeasurementDto> Measurements,
    string DominantPollutant
);
