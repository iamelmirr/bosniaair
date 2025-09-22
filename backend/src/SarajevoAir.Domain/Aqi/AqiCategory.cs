namespace SarajevoAir.Domain.Aqi;

public enum AqiCategory
{
    Good,
    Moderate,
    USG, // Unhealthy for Sensitive Groups
    Unhealthy,
    VeryUnhealthy,
    Hazardous
}

public record AqiResult(
    int Aqi,
    AqiCategory Category,
    Dictionary<string, int> Subindices
);

public static class AqiCategoryExtensions
{
    public static string GetDisplayName(this AqiCategory category) => category switch
    {
        AqiCategory.Good => "Good",
        AqiCategory.Moderate => "Moderate",
        AqiCategory.USG => "Unhealthy for Sensitive Groups",
        AqiCategory.Unhealthy => "Unhealthy",
        AqiCategory.VeryUnhealthy => "Very Unhealthy",
        AqiCategory.Hazardous => "Hazardous",
        _ => "Unknown"
    };

    public static string GetHexColor(this AqiCategory category) => category switch
    {
        AqiCategory.Good => "#00E400",
        AqiCategory.Moderate => "#FFFF00", 
        AqiCategory.USG => "#FF7E00",
        AqiCategory.Unhealthy => "#FF0000",
        AqiCategory.VeryUnhealthy => "#99004C",
        AqiCategory.Hazardous => "#7E0023",
        _ => "#6B7280"
    };

    public static string GetRecommendation(this AqiCategory category, string language = "en") =>
        language.ToLower() switch
        {
            "bs" or "hr" or "sr" => category switch
            {
                AqiCategory.Good => "Zrak je dobar. Uživajte vani!",
                AqiCategory.Moderate => "Umjerena zagađenja. Osjetljivima oprez pri naporu.",
                AqiCategory.USG => "Osjetljive grupe neka reduciraju intenzivne aktivnosti vani.",
                AqiCategory.Unhealthy => "Nezdravo. Izbjegavajte naporne aktivnosti vani.",
                AqiCategory.VeryUnhealthy => "Vrlo nezdravo. Preporučeno ostati u zatvorenom.",
                AqiCategory.Hazardous => "Opasno. Ostanite u zatvorenom i koristite zaštitu.",
                _ => "Podaci nisu dostupni."
            },
            _ => category switch
            {
                AqiCategory.Good => "Air quality is satisfactory. Enjoy outdoor activities!",
                AqiCategory.Moderate => "Air quality is acceptable. Sensitive individuals should consider limiting prolonged outdoor exertion.",
                AqiCategory.USG => "Sensitive groups should reduce prolonged or heavy outdoor exertion.",
                AqiCategory.Unhealthy => "Everyone should reduce prolonged or heavy outdoor exertion.",
                AqiCategory.VeryUnhealthy => "Everyone should avoid prolonged or heavy outdoor exertion.",
                AqiCategory.Hazardous => "Everyone should avoid all outdoor exertion.",
                _ => "Data unavailable."
            }
        };
}