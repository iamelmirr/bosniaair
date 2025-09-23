using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SarajevoAir.Application.Dtos;
using SarajevoAir.Domain.Aqi;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<GroupsController> _logger;
    private readonly IAqicnClient _aqicnClient;

    public GroupsController(IMemoryCache cache, ILogger<GroupsController> logger, IAqicnClient aqicnClient)
    {
        _cache = cache;
        _logger = logger;
        _aqicnClient = aqicnClient;
    }

    /// <summary>
    /// Get health recommendations for different user groups based on current AQI
    /// </summary>
    /// <returns>Health recommendations for different groups with current AQI data</returns>
    [HttpGet]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes (same as live data)
    public async Task<IActionResult> GetGroupRecommendations([FromQuery] string city = "sarajevo")
    {
        try
        {
            var cacheKey = $"groups-{city}";
            
            if (_cache.TryGetValue(cacheKey, out var cachedData))
            {
                return Ok(cachedData);
            }

            // Get current AQI data
            var aqiData = await _aqicnClient.GetCityDataAsync(city);
            if (aqiData == null)
            {
                return NotFound($"No AQI data found for city: {city}");
            }

            var currentAqi = aqiData.Data.Aqi;
            var aqiCategory = GetAqiCategoryName(currentAqi);

            // Generate groups with current recommendations
            var groups = new List<object>
            {
                CreateGroupRecommendation("Sportisti", currentAqi, aqiCategory),
                CreateGroupRecommendation("Djeca", currentAqi, aqiCategory),
                CreateGroupRecommendation("Stariji", currentAqi, aqiCategory),
                CreateGroupRecommendation("Astmatičari", currentAqi, aqiCategory)
            };

            var response = new
            {
                city = aqiData.Data.City.Name ?? city,
                currentAqi = currentAqi,
                aqiCategory = aqiCategory,
                groups = groups,
                timestamp = DateTime.UtcNow
            };

            // Cache for 5 minutes
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Generated group recommendations for {City} with AQI {Aqi}", city, currentAqi);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get group recommendations for city {City}", city);
            return StatusCode(500, new { 
                message = "Failed to retrieve group recommendations"
            });
        }
    }

    private static object CreateGroupRecommendation(string groupName, int currentAqi, string aqiCategory)
    {
        var group = new
        {
            groupName = groupName,
            aqiThreshold = GetAqiThreshold(groupName),
            recommendations = GetRecommendationsForGroup(groupName),
            iconEmoji = GetGroupIcon(groupName),
            description = GetGroupDescription(groupName)
        };

        var currentRecommendation = GetCurrentRecommendation(groupName, aqiCategory);
        var riskLevel = GetRiskLevel(currentAqi, groupName);

        return new
        {
            group = group,
            currentRecommendation = currentRecommendation,
            riskLevel = riskLevel
        };
    }

    private static string GetAqiCategoryName(int aqi)
    {
        return aqi switch
        {
            >= 0 and <= 50 => "Good",
            >= 51 and <= 100 => "Moderate", 
            >= 101 and <= 150 => "Unhealthy for Sensitive Groups",
            >= 151 and <= 200 => "Unhealthy",
            >= 201 and <= 300 => "Very Unhealthy",
            _ => "Hazardous"
        };
    }

    private static int GetAqiThreshold(string groupName)
    {
        return groupName switch
        {
            "Sportisti" => 100,
            "Djeca" => 75,
            "Stariji" => 75,
            "Astmatičari" => 50,
            _ => 100
        };
    }

    private static object GetRecommendationsForGroup(string groupName)
    {
        return groupName switch
        {
            "Sportisti" => new
            {
                good = "Idealno vrijeme za sve sportske aktivnosti. Uživajte u treningu vani!",
                moderate = "Dobro za većinu aktivnosti. Kraće pauze ako osjećate nelagodu.",
                unhealthyForSensitive = "Ograničite intenzivne treninge. Preferirajte zatvorene prostore.",
                unhealthy = "Izbjegavajte outdoor treninge. Koristite teretane i zatvorene objekte.",
                veryUnhealthy = "Sve aktivnosti samo u zatvorenim prostorima s filtracijom zraka.",
                hazardous = "Otkazujte sve outdoor aktivnosti. Ostanite u zatvorenom."
            },
            "Djeca" => new
            {
                good = "Djeca mogu nesmetano igrati vani. Poticajte outdoor aktivnosti.",
                moderate = "Većina djece može igrati vani, ali pazite na one s respiratornim problemima.",
                unhealthyForSensitive = "Ograničite vrijeme vani za svu djecu. Kratke šetnje su OK.",
                unhealthy = "Djeca treba da ostanu u zatvorenim prostorima. Izbjegavajte outdoor aktivnosti.",
                veryUnhealthy = "Sve djeca unutra. Zatvorite prozore, koristite prečišćivače zraka.",
                hazardous = "Hitno: sva djeca ostaju u zatvorenim prostorima. Nositi maske ako je potrebno izaći."
            },
            "Stariji" => new
            {
                good = "Sigurno za sve aktivnosti vani. Dobro vrijeme za šetnje i vrt.",
                moderate = "Ograničite naporne aktivnosti vani. Kratke šetnje su u redu.",
                unhealthyForSensitive = "Ostanite unutra ako imate bolesti srca ili pluća.",
                unhealthy = "Svi stariji ostaju u zatvorenom. Izbjegavajte sve outdoor aktivnosti.",
                veryUnhealthy = "Ostanite unutra. Zatvorite prozore. Kontaktirajte ljekara pri problemima.",
                hazardous = "Hitno: ostanite u zatvorenom. Pozovite ljekara ako osjećate simptome."
            },
            "Astmatičari" => new
            {
                good = "Sigurno za sve aktivnosti. Redovito uzimajte lijekove.",
                moderate = "Oprez pri fizičkim aktivnostima. Imajte inhalator pri ruci.",
                unhealthyForSensitive = "Ograničite aktivnosti vani. Povećajte dozu lijekova ako je preporučeno.",
                unhealthy = "Ostanite u zatvorenom. Koristite inhalator preporučeno. Kontakt s ljekarom.",
                veryUnhealthy = "Ostanite unutra. Pripremite rescue medikacije. Pozovite ljekara.",
                hazardous = "Hitno ostanite unutra. Imajte emergency lijekove. Pozovite hitnu ako je potrebno."
            },
            _ => new { good = "", moderate = "", unhealthyForSensitive = "", unhealthy = "", veryUnhealthy = "", hazardous = "" }
        };
    }

    private static string GetGroupIcon(string groupName)
    {
        return groupName switch
        {
            "Sportisti" => "🏃‍♂️",
            "Djeca" => "👶",
            "Stariji" => "👴",
            "Astmatičari" => "🫁",
            _ => "👤"
        };
    }

    private static string GetGroupDescription(string groupName)
    {
        return groupName switch
        {
            "Sportisti" => "Preporuke za sportske aktivnosti i vežbanje na osnovu kvaliteta zraka",
            "Djeca" => "Posebne preporuke za zaštitu djece od zagađenja zraka",
            "Stariji" => "Savjeti za starije osobe (65+) i one s kroničnim bolestima",
            "Astmatičari" => "Specijalni savjeti za astmatičare i osobe s respiratornim problemima",
            _ => "Zdravstvene preporuke na osnovu kvaliteta zraka"
        };
    }

    private static string GetCurrentRecommendation(string groupName, string aqiCategory)
    {
        var recommendations = GetRecommendationsForGroup(groupName);
        
        return aqiCategory switch
        {
            "Good" => GetRecommendationProperty(recommendations, "good"),
            "Moderate" => GetRecommendationProperty(recommendations, "moderate"),
            "Unhealthy for Sensitive Groups" => GetRecommendationProperty(recommendations, "unhealthyForSensitive"),
            "Unhealthy" => GetRecommendationProperty(recommendations, "unhealthy"),
            "Very Unhealthy" => GetRecommendationProperty(recommendations, "veryUnhealthy"),
            "Hazardous" => GetRecommendationProperty(recommendations, "hazardous"),
            _ => "Provjerite kvalitet zraka prije izlaska."
        };
    }

    private static string GetRecommendationProperty(object recommendations, string propertyName)
    {
        var property = recommendations.GetType().GetProperty(propertyName);
        return property?.GetValue(recommendations)?.ToString() ?? "";
    }

    private static string GetRiskLevel(int aqi, string groupName)
    {
        var threshold = GetAqiThreshold(groupName);
        
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
}