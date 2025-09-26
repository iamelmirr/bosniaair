/*
=== UTC DATETIME JSON CONVERTER ===
Fixes timezone issue where DateTime objects are serialized without timezone info

PROBLEM:
- Backend koristi lokalno Sarajevo vrijeme (UTC+2)
- JSON serializer serijalizuje kao "2025-09-26T13:24:35.074576" (no timezone info)
- Frontend parsira kao lokalnu timezone umjesto UTC

SOLUTION:
- Custom converter koji uvijek dodaje "Z" za UTC
- Frontend će pravilno parsirati UTC vrijeme
- Intl.DateTimeFormat će konvertirati u lokalnu timezone za display
*/

using System.Text.Json;
using System.Text.Json.Serialization;
using SarajevoAir.Api.Utilities;

namespace SarajevoAir.Api.Configuration;

/// <summary>
/// Custom JSON converter koji osigurava da se DateTime objekti uvijek serijaliziraju kao UTC
/// Dodaje "Z" suffix za ISO 8601 UTC format
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (DateTime.TryParse(value, out var date))
        {
            return date.ToUniversalTime();
        }
        return TimeZoneHelper.GetSarajevoTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Always convert to UTC and add 'Z' suffix for proper timezone indication
        var utcDateTime = value.Kind == DateTimeKind.Unspecified ? 
            DateTime.SpecifyKind(value, DateTimeKind.Utc) : 
            value.ToUniversalTime();
            
        writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"));
    }
}