using System.Linq;
using Microsoft.Extensions.Logging;
using SarajevoAir.Api.Dtos;

namespace SarajevoAir.Api.Services;

public interface IHealthAdviceService
{
    Task<GroupsResponse> BuildGroupsResponseAsync(string city, CancellationToken cancellationToken = default);
}

public class HealthAdviceService : IHealthAdviceService
{
    private readonly IAirQualityService _airQualityService;
    private readonly ILogger<HealthAdviceService> _logger;

    private static readonly string[] GroupNames = ["Sportisti", "Djeca", "Stariji", "Astmatičari"];

    public HealthAdviceService(IAirQualityService airQualityService, ILogger<HealthAdviceService> logger)
    {
        _airQualityService = airQualityService;
        _logger = logger;
    }

    public async Task<GroupsResponse> BuildGroupsResponseAsync(string city, CancellationToken cancellationToken = default)
    {
        var live = await _airQualityService.GetLiveAqiAsync(city, forceFresh: false, cancellationToken);

        var groupStatuses = GroupNames
            .Select(name => BuildGroupStatus(name, live.OverallAqi))
            .ToList();

        _logger.LogInformation("Generated health guidance for {City} at AQI {Aqi}", live.City, live.OverallAqi);

        return new GroupsResponse(
            City: live.City,
            CurrentAqi: live.OverallAqi,
            AqiCategory: live.AqiCategory,
            Groups: groupStatuses,
            Timestamp: DateTime.UtcNow
        );
    }

    private static GroupStatusDto BuildGroupStatus(string groupName, int currentAqi)
    {
        var group = new HealthGroupDto(
            GroupName: groupName,
            AqiThreshold: GetThreshold(groupName),
            Recommendations: GetRecommendations(groupName),
            IconEmoji: GetIcon(groupName),
            Description: GetDescription(groupName)
        );

        var recommendation = GetCurrentRecommendation(group.Recommendations, currentAqi);
        var riskLevel = GetRiskLevel(currentAqi, groupName);

        return new GroupStatusDto(group, recommendation, riskLevel);
    }

    private static GroupRecommendations GetRecommendations(string groupName) => groupName switch
    {
        "Sportisti" => new GroupRecommendations(
            Good: "Idealno vrijeme za sve sportske aktivnosti. Uživajte u treningu vani!",
            Moderate: "Dobro za većinu aktivnosti. Kraće pauze ako osjećate nelagodu.",
            UnhealthyForSensitive: "Ograničite intenzivne treninge. Preferirajte zatvorene prostore.",
            Unhealthy: "Izbjegavajte outdoor treninge. Koristite teretane i zatvorene objekte.",
            VeryUnhealthy: "Sve aktivnosti samo u zatvorenim prostorima s filtracijom zraka.",
            Hazardous: "Otkazujte sve outdoor aktivnosti. Ostanite u zatvorenom."),
        "Djeca" => new GroupRecommendations(
            Good: "Djeca mogu nesmetano igrati vani. Poticajte outdoor aktivnosti.",
            Moderate: "Većina djece može igrati vani, ali pazite na one s respiratornim problemima.",
            UnhealthyForSensitive: "Ograničite vrijeme vani za svu djecu. Kratke šetnje su OK.",
            Unhealthy: "Djeca treba da ostanu u zatvorenim prostorima. Izbjegavajte outdoor aktivnosti.",
            VeryUnhealthy: "Sve djeca unutra. Zatvorite prozore, koristite prečišćivače zraka.",
            Hazardous: "Hitno: sva djeca ostaju u zatvorenim prostorima. Nositi maske ako je potrebno izaći."),
        "Stariji" => new GroupRecommendations(
            Good: "Sigurno za sve aktivnosti vani. Dobro vrijeme za šetnje i vrt.",
            Moderate: "Ograničite naporne aktivnosti vani. Kratke šetnje su u redu.",
            UnhealthyForSensitive: "Ostanite unutra ako imate bolesti srca ili pluća.",
            Unhealthy: "Svi stariji ostaju u zatvorenom. Izbjegavajte sve outdoor aktivnosti.",
            VeryUnhealthy: "Ostanite unutra. Zatvorite prozore. Kontaktirajte ljekara pri problemima.",
            Hazardous: "Hitno: ostanite u zatvorenom. Pozovite ljekara ako osjećate simptome."),
        "Astmatičari" => new GroupRecommendations(
            Good: "Sigurno za sve aktivnosti. Redovito uzimajte lijekove.",
            Moderate: "Oprez pri fizičkim aktivnostima. Imajte inhalator pri ruci.",
            UnhealthyForSensitive: "Ograničite aktivnosti vani. Povećajte dozu lijekova ako je preporučeno.",
            Unhealthy: "Ostanite u zatvorenom. Koristite inhalator preporučeno. Kontakt s ljekarom.",
            VeryUnhealthy: "Ostanite unutra. Pripremite rescue medikacije. Pozovite ljekara.",
            Hazardous: "Hitno ostanite unutra. Imajte emergency lijekove. Pozovite hitnu ako je potrebno."),
        _ => new GroupRecommendations("", "", "", "", "", "")
    };

    private static string GetCurrentRecommendation(GroupRecommendations recommendations, int aqi)
    {
        var category = GetAqiCategory(aqi);
        return category switch
        {
            "Good" => recommendations.Good,
            "Moderate" => recommendations.Moderate,
            "Unhealthy for Sensitive Groups" => recommendations.UnhealthyForSensitive,
            "Unhealthy" => recommendations.Unhealthy,
            "Very Unhealthy" => recommendations.VeryUnhealthy,
            "Hazardous" => recommendations.Hazardous,
            _ => "Provjerite kvalitet zraka prije izlaska."
        };
    }

    private static string GetRiskLevel(int aqi, string groupName)
    {
        var threshold = GetThreshold(groupName);
        return aqi switch
        {
            <= 50 => "low",
            <= 100 when aqi <= threshold => "low",
            <= 100 => "moderate",
            <= 150 => "moderate",
            <= 200 => "high",
            _ => "very-high"
        };
    }

    private static int GetThreshold(string groupName) => groupName switch
    {
        "Sportisti" => 100,
        "Djeca" => 75,
        "Stariji" => 75,
        "Astmatičari" => 50,
        _ => 100
    };

    private static string GetIcon(string groupName) => groupName switch
    {
        "Sportisti" => "🏃‍♂️",
        "Djeca" => "👶",
        "Stariji" => "👴",
        "Astmatičari" => "🫁",
        _ => "👤"
    };

    private static string GetDescription(string groupName) => groupName switch
    {
        "Sportisti" => "Preporuke za sportske aktivnosti i vežbanje na osnovu kvaliteta zraka",
        "Djeca" => "Posebne preporuke za zaštitu djece od zagađenja zraka",
        "Stariji" => "Savjeti za starije osobe (65+) i one s kroničnim bolestima",
        "Astmatičari" => "Specijalni savjeti za astmatičare i osobe s respiratornim problemima",
        _ => "Zdravstvene preporuke na osnovu kvaliteta zraka"
    };

    private static string GetAqiCategory(int aqi) => aqi switch
    {
        <= 50 => "Good",
        <= 100 => "Moderate",
        <= 150 => "Unhealthy for Sensitive Groups",
        <= 200 => "Unhealthy",
        <= 300 => "Very Unhealthy",
        _ => "Hazardous"
    };
}
