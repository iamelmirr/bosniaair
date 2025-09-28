using System.Text.Json;
using System.Text.Json.Serialization;
using SarajevoAir.Api.Utilities;

namespace SarajevoAir.Api.Configuration;
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
        var utcDateTime = value.Kind == DateTimeKind.Unspecified ? 
            DateTime.SpecifyKind(value, DateTimeKind.Utc) : 
            value.ToUniversalTime();
            
        writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"));
    }
}