namespace BosniaAir.Api.Dtos;

/// <summary>
/// Geographic coordinate data
/// </summary>
public record CoordinateDto(double Latitude, double Longitude);

/// <summary>
/// Individual air pollutant measurement data
/// </summary>
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

/// <summary>
/// Time period over which the measurement was averaged
/// </summary>
public record AveragingPeriodDto(double Value, string Unit);

/// <summary>
/// Response containing live air quality data for a city
/// </summary>
public record LiveAqiResponse(
    string City = "",
    int OverallAqi = 0,
    string AqiCategory = "",
    string Color = "",
    string HealthMessage = "",
    DateTime Timestamp = default,
    IReadOnlyList<MeasurementDto> Measurements = null!,
    string DominantPollutant = ""
);
