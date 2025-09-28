using System.Collections.Generic;
using System.Globalization;

namespace SarajevoAir.Api.Enums;

public static class CityExtensions
{
    private static readonly IReadOnlyDictionary<City, string> StationIds = new Dictionary<City, string>
    {
        { City.Sarajevo, "@10557" },
        { City.Tuzla, "A462985" },
        { City.Zenica, "@9267" },
        { City.Mostar, "@14726" },
        { City.Travnik, "@14693" },
        { City.Bihac, "@13578" }
    };

    public static string ToStationId(this City city)
    {
        if (StationIds.TryGetValue(city, out var stationId))
        {
            return stationId;
        }

        return $"@{(int)city}";
    }

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
