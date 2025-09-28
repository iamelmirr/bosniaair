using System.Collections.Generic;
using System.Globalization;

namespace SarajevoAir.Api.Enums;

/// <summary>
/// Extension methods for the City enum providing utility functions.
/// </summary>
public static class CityExtensions
{
    /// <summary>
    /// Mapping of cities to their WAQI API station identifiers.
    /// Some cities use custom station IDs while others use the enum value prefixed with @.
    /// </summary>
    private static readonly IReadOnlyDictionary<City, string> StationIds = new Dictionary<City, string>
    {
        { City.Sarajevo, "@10557" },
        { City.Tuzla, "A462985" },
        { City.Zenica, "@9267" },
        { City.Mostar, "@14726" },
        { City.Travnik, "@14693" },
        { City.Bihac, "@13578" }
    };

    /// <summary>
    /// Converts a City enum value to its corresponding WAQI station ID string.
    /// </summary>
    /// <param name="city">The city to convert</param>
    /// <returns>The WAQI station ID for the city</returns>
    public static string ToStationId(this City city)
    {
        if (StationIds.TryGetValue(city, out var stationId))
        {
            return stationId;
        }

        return $"@{(int)city}";
    }

    /// <summary>
    /// Converts a City enum value to a properly formatted display name.
    /// </summary>
    /// <param name="city">The city to convert</param>
    /// <returns>The display name for the city</returns>
    public static string ToDisplayName(this City city)
    {
        var name = city.ToString();
        return name switch
        {
            nameof(City.Travnik) => "Travnik",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLowerInvariant())
        };
    }
}
