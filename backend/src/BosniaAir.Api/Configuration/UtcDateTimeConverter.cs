using System.Text.Json;
using System.Text.Json.Serialization;
using BosniaAir.Api.Utilities;

namespace BosniaAir.Api.Configuration;

/// <summary>
/// JSON converter for DateTime values that ensures UTC serialization and proper parsing.
/// Handles conversion between local times and UTC for API communication.
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    /// <summary>
    /// Reads a DateTime from JSON, converting it to UTC.
    /// If parsing fails, returns current Sarajevo time as fallback.
    /// </summary>
    /// <param name="reader">The JSON reader</param>
    /// <param name="typeToConvert">The type being converted</param>
    /// <param name="options">Serialization options</param>
    /// <returns>The parsed DateTime in UTC</returns>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (DateTime.TryParse(value, out var date))
        {
            return date.ToUniversalTime();
        }
        return TimeZoneHelper.GetSarajevoTime();
    }

    /// <summary>
    /// Writes a DateTime to JSON in UTC format with high precision.
    /// </summary>
    /// <param name="writer">The JSON writer</param>
    /// <param name="value">The DateTime value to write</param>
    /// <param name="options">Serialization options</param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utcDateTime = value.Kind == DateTimeKind.Unspecified ? 
            DateTime.SpecifyKind(value, DateTimeKind.Utc) : 
            value.ToUniversalTime();
            
        writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"));
    }
}