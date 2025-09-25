namespace SarajevoAir.Api.Configuration;

public class AqicnConfiguration
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    
    // Static mapping of cities to their specific station IDs
    public static readonly Dictionary<string, string> CityStations = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Sarajevo", "@9265" },
        { "Tuzla", "A462985" },
        { "Mostar", "@14726" },
        { "Banja Luka", "A84268" },
        { "Zenica", "@9267" },
        { "Bihac", "@13578" }
    };
}