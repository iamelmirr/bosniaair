using System.Globalization;

namespace SarajevoAir.Api.Enums;

public static class CityExtensions
{
    /// <summary>
    /// Returns WAQI station identifier for the city (prefixed with '@').
    /// </summary>
    public static string ToStationId(this City city) => $"@{(int)city}";

    /// <summary>
    /// Returns a user friendly display name (adds space between words).
    /// </summary>
    public static string ToDisplayName(this City city)
    {
        var name = city.ToString();
        return name switch
        {
            nameof(City.BanjaLuka) => "Banja Luka",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLowerInvariant())
        };
    }
}
