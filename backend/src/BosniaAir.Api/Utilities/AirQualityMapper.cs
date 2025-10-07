using System.Globalization;
using BosniaAir.Api.Dtos;
using BosniaAir.Api.Entities;
using BosniaAir.Api.Enums;

namespace BosniaAir.Api.Utilities;

/// <summary>
/// Centralized mapper for all air quality data transformations.
/// Handles Entity ↔ DTO conversions, WAQI data parsing, and response building.
/// </summary>
public static class AirQualityMapper
{
    /// <summary>
    /// Maps AirQualityRecord entity to LiveAqiResponse DTO.
    /// </summary>
    public static LiveAqiResponse MapToLiveResponse(AirQualityRecord record)
    {
        var aqi = record.AqiValue ?? 0;
        var (category, color, message) = GetAqiInfo(aqi);
        var measurements = BuildMeasurements(record);

        return new LiveAqiResponse(
            City: record.City.ToDisplayName(),
            OverallAqi: aqi,
            AqiCategory: category,
            Color: color,
            HealthMessage: message,
            Timestamp: record.Timestamp,
            Measurements: measurements,
            DominantPollutant: record.DominantPollutant ?? "Unknown"
        );
    }

    /// <summary>
    /// Maps WAQI data to AirQualityRecord entity.
    /// </summary>
    public static AirQualityRecord MapToEntity(City city, WaqiData waqiData, DateTime timestamp)
    {
        return new AirQualityRecord
        {
            City = city,
            StationId = city.ToStationId(),
            RecordType = AirQualityRecordType.LiveSnapshot,
            Timestamp = timestamp,
            AqiValue = waqiData.Aqi,
            DominantPollutant = MapDominantPollutant(waqiData.Dominentpol),
            Pm25 = waqiData.Iaqi?.Pm25?.V,
            Pm10 = waqiData.Iaqi?.Pm10?.V,
            O3 = waqiData.Iaqi?.O3?.V,
            No2 = waqiData.Iaqi?.No2?.V,
            Co = waqiData.Iaqi?.Co?.V,
            So2 = waqiData.Iaqi?.So2?.V,
            CreatedAt = TimeZoneHelper.GetSarajevoTime()
        };
    }

    /// <summary>
    /// Builds forecast response from WAQI forecast data.
    /// </summary>
    public static ForecastResponse? BuildForecastResponse(City city, WaqiDailyForecast? dailyForecast, DateTime timestamp)
    {
        if (dailyForecast is null)
        {
            return null;
        }

        var forecastDays = BuildForecastDays(dailyForecast);
        
        if (forecastDays.Count == 0)
        {
            return null;
        }

        return new ForecastResponse(city.ToDisplayName(), forecastDays, timestamp);
    }

    /// <summary>
    /// Parses WAQI timestamp to Sarajevo local time.
    /// Prefers ISO string format, falls back to Unix timestamp.
    /// </summary>
    public static DateTime ParseTimestamp(WaqiTime time)
    {
        // Try ISO format first
        if (!string.IsNullOrWhiteSpace(time.Iso) && 
            DateTime.TryParse(time.Iso, CultureInfo.InvariantCulture, 
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedIso))
        {
            return TimeZoneHelper.ConvertToSarajevoTime(parsedIso.ToUniversalTime());
        }

        // Fallback to Unix timestamp
        if (time.V > 0)
        {
            var unixEpoch = DateTimeOffset.FromUnixTimeSeconds(time.V);
            return TimeZoneHelper.ConvertToSarajevoTime(unixEpoch.UtcDateTime);
        }

        // Last resort: current Sarajevo time
        return TimeZoneHelper.GetSarajevoTime();
    }

    /// <summary>
    /// Builds measurement DTOs from pollutant values in the record.
    /// Only includes measurements with non-null values.
    /// </summary>
    private static IReadOnlyList<MeasurementDto> BuildMeasurements(AirQualityRecord record)
    {
        var measurements = new List<MeasurementDto>();

        void AddMeasurement(string parameter, double? value, string unit)
        {
            if (!value.HasValue)
            {
                return;
            }

            measurements.Add(new MeasurementDto(
                Id: $"{record.Id}_{parameter.ToLowerInvariant()}",
                City: record.City.ToDisplayName(),
                LocationName: record.City.ToDisplayName(),
                Parameter: parameter,
                Value: value.Value,
                Unit: unit,
                Timestamp: record.Timestamp,
                SourceName: "WAQI",
                Coordinates: null,
                AveragingPeriod: null
            ));
        }

        AddMeasurement("PM2.5", record.Pm25, "μg/m³");
        AddMeasurement("PM10", record.Pm10, "μg/m³");
        AddMeasurement("O3", record.O3, "μg/m³");
        AddMeasurement("NO2", record.No2, "μg/m³");
        AddMeasurement("CO", record.Co, "mg/m³");
        AddMeasurement("SO2", record.So2, "μg/m³");

        return measurements;
    }

    /// <summary>
    /// Processes WAQI forecast data into forecast day DTOs.
    /// Merges pollutants by date and filters to include only future days.
    /// </summary>
    private static List<ForecastDayDto> BuildForecastDays(WaqiDailyForecast forecast)
    {
        var map = new Dictionary<DateOnly, ForecastDayData>();

        // Merge PM2.5 entries
        MergeEntries(forecast.Pm25, map, (data, range) => data.Pm25 = range);
        
        // Merge PM10 entries
        MergeEntries(forecast.Pm10, map, (data, range) => data.Pm10 = range);
        
        // Merge O3 entries
        MergeEntries(forecast.O3, map, (data, range) => data.O3 = range);

        // Build sorted results, filtering future days only
        return BuildSortedResults(map);
    }

    /// <summary>
    /// Merges forecast entries for a specific pollutant into the date map.
    /// </summary>
    private static void MergeEntries(
        WaqiForecastEntry[]? entries,
        Dictionary<DateOnly, ForecastDayData> map,
        Action<ForecastDayData, PollutantRangeDto> assign)
    {
        if (entries is null)
        {
            return;
        }

        foreach (var entry in entries)
        {
            if (!DateOnly.TryParseExact(entry.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            {
                continue;
            }

            if (!map.TryGetValue(day, out var data))
            {
                data = new ForecastDayData();
                map[day] = data;
            }

            var range = new PollutantRangeDto(
                Avg: ToInt(entry.Avg),
                Min: ToInt(entry.Min),
                Max: ToInt(entry.Max)
            );

            assign(data, range);
        }
    }

    /// <summary>
    /// Builds sorted forecast results from the date map, filtering to include only future days.
    /// </summary>
    private static List<ForecastDayDto> BuildSortedResults(Dictionary<DateOnly, ForecastDayData> map)
    {
        var ordered = map.OrderBy(kvp => kvp.Key).ToList();
        var sarajevoToday = DateOnly.FromDateTime(TimeZoneHelper.GetSarajevoTime());
        var results = new List<ForecastDayDto>();

        foreach (var (date, data) in ordered)
        {
            if (date >= sarajevoToday)
            {
                var representativeAqi = data.Pm25?.Avg ?? data.Pm10?.Avg ?? data.O3?.Avg ?? 0;
                var (category, color, _) = GetAqiInfo(representativeAqi);
                var forecastPollutants = new ForecastDayPollutants(data.Pm25, data.Pm10, data.O3);

                results.Add(new ForecastDayDto(
                    Date: date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Aqi: representativeAqi,
                    Category: category,
                    Color: color,
                    Pollutants: forecastPollutants
                ));
            }
        }

        return results;
    }

    /// <summary>
    /// Rounds a double to int using away-from-zero rounding.
    /// </summary>
    private static int ToInt(double value) => Convert.ToInt32(Math.Round(value, MidpointRounding.AwayFromZero));

    /// <summary>
    /// Temporary holder for merging forecast pollutants by date.
    /// </summary>
    private sealed class ForecastDayData
    {
        public PollutantRangeDto? Pm25 { get; set; }
        public PollutantRangeDto? Pm10 { get; set; }
        public PollutantRangeDto? O3 { get; set; }
    }

    #region AQI Category Helpers

    /// <summary>
    /// Gets the AQI category information based on EPA standards.
    /// </summary>
    private static (string Category, string Color, string Message) GetAqiInfo(int aqi) => aqi switch
    {
        <= 50 => (
            "Good",
            "#00E400",
            "Air quality is considered satisfactory, and air pollution poses little or no risk."
        ),
        <= 100 => (
            "Moderate",
            "#FFFF00",
            "Air quality is acceptable for most people. However, for some pollutants there may be a moderate health concern for a very small number of people who are unusually sensitive to air pollution."
        ),
        <= 150 => (
            "Unhealthy for Sensitive Groups",
            "#FF7E00",
            "Members of sensitive groups may experience health effects. The general public is not likely to be affected."
        ),
        <= 200 => (
            "Unhealthy",
            "#FF0000",
            "Everyone may begin to experience health effects; members of sensitive groups may experience more serious health effects."
        ),
        <= 300 => (
            "Very Unhealthy",
            "#8F3F97",
            "Health warnings of emergency conditions. The entire population is more likely to be affected."
        ),
        _ => (
            "Hazardous",
            "#7E0023",
            "Health alert: everyone may experience more serious health effects."
        )
    };

    /// <summary>
    /// Maps WAQI pollutant codes to human-readable names.
    /// </summary>
    private static string MapDominantPollutant(string? pollutant) => pollutant?.ToLowerInvariant() switch
    {
        "pm25" => "PM2.5",
        "pm10" => "PM10",
        "no2" => "NO2",
        "o3" => "O3",
        "so2" => "SO2",
        "co" => "CO",
        _ => "Unknown"
    };

    #endregion
}
