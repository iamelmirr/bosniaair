using System.Collections.Generic;
using System.Globalization;

namespace SarajevoAir.Api.Enums;

public static class CityExtensions
{
    private static readonly IReadOnlyDictionary<City, string> StationIds = new Dictionary<City, string>
    {
        { City.Sarajevo, "@10557" },
        { City.Tuzla, "@9321" },
        { City.Zenica, "@9267" },
        { City.Mostar, "@14726" },
        { City.Vitez, "A475627" },
        { City.Bihac, "@13578" }
    };

    /// <summary>
    /// Returns WAQI station identifier for the city (prefixed with '@' or alternative format).
    /// </summary>
    public static string ToStationId(this City city)
    {
        if (StationIds.TryGetValue(city, out var stationId))
        {
            return stationId;
        }

        return $"@{(int)city}";
    }

    /// <summary>
    /// Returns a user friendly display name (adds space between words).
    /// </summary>
    public static string ToDisplayName(this City city)
    {
        var name = city.ToString();
        return name switch
        {
            nameof(City.Vitez) => "Vitez",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLowerInvariant())
        };
    }
}
