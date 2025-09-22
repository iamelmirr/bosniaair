using System.Text.Json.Serialization;

namespace SarajevoAir.Infrastructure.OpenAq.Models;

public record OpenAqResponse<T>(
    [property: JsonPropertyName("results")] List<T>? Results,
    [property: JsonPropertyName("meta")] OpenAqMeta? Meta
);

public record OpenAqMeta(
    [property: JsonPropertyName("found")] int Found,
    [property: JsonPropertyName("limit")] int Limit,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("pages")] int Pages
);

public record OpenAqLocation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("coordinates")] OpenAqCoordinates? Coordinates,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("isMobile")] bool? IsMobile,
    [property: JsonPropertyName("isAnalysis")] bool? IsAnalysis
);

public record OpenAqCoordinates(
    [property: JsonPropertyName("latitude")] double? Latitude,
    [property: JsonPropertyName("longitude")] double? Longitude
);

public record OpenAqSensor(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("parameter")] string? Parameter,
    [property: JsonPropertyName("unit")] string? Unit,
    [property: JsonPropertyName("sensorType")] string? SensorType
);

public record OpenAqMeasurement(
    [property: JsonPropertyName("sensorId")] long SensorId,
    [property: JsonPropertyName("value")] double? Value,
    [property: JsonPropertyName("parameter")] string? Parameter,
    [property: JsonPropertyName("unit")] string? Unit,
    [property: JsonPropertyName("datetimeUtc")] DateTime? DatetimeUtc,
    [property: JsonPropertyName("coordinates")] OpenAqCoordinates? Coordinates
);