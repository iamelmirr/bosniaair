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
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("coordinates")] OpenAqCoordinates? Coordinates,
    [property: JsonPropertyName("country")] OpenAqCountry? Country,
    [property: JsonPropertyName("locality")] string? Locality,
    [property: JsonPropertyName("timezone")] string? Timezone,
    [property: JsonPropertyName("isMobile")] bool? IsMobile,
    [property: JsonPropertyName("isMonitor")] bool? IsMonitor
);

public record OpenAqCountry(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name
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