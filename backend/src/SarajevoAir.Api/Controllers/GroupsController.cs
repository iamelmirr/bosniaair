using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SarajevoAir.Application.Dtos;
using SarajevoAir.Domain.Aqi;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IMemoryCache cache, ILogger<GroupsController> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get health recommendations for different user groups based on AQI categories
    /// </summary>
    /// <returns>Health recommendations for different groups</returns>
    [HttpGet]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour
    public IActionResult GetGroupRecommendations()
    {
        try
        {
            const string cacheKey = "group-recommendations";
            
            if (_cache.TryGetValue(cacheKey, out var cachedData))
            {
                return Ok(cachedData);
            }

            var recommendations = new List<GroupRecommendationDto>
            {
                new("Sportisti", "Preporuke za sportske aktivnosti", 
                    "Vodič za sigurno vežbanje na osnovu kvaliteta zraka",
                    GetSportsRecommendations()),
                
                new("Djeca", "Preporuke za djecu", 
                    "Posebne preporuke za zaštitu djece od zagađenja zraka",
                    GetChildrenRecommendations()),
                
                new("Stariji", "Preporuke za starije osobe", 
                    "Savjeti za starije osobe (65+) i one s kroničnim bolestima",
                    GetElderlyRecommendations()),
                
                new("Astmatičari", "Preporuke za osobe s astmom", 
                    "Specijalni savjeti za astmatičare i osobe s respiratornim problemima",
                    GetAsthmaRecommendations())
            };

            // Cache for 1 hour
            _cache.Set(cacheKey, recommendations, TimeSpan.FromHours(1));
            
            _logger.LogInformation("Generated group recommendations for {Count} groups", recommendations.Count);
            
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get group recommendations");
            return StatusCode(500, new { 
                message = "Failed to retrieve group recommendations"
            });
        }
    }

    private static List<CategoryRecommendation> GetSportsRecommendations() =>
        new()
        {
            new("Good", "Idealno vrijeme za sve sportske aktivnosti. Uživajte u treningu vani!", "safe"),
            new("Moderate", "Dobro za većinu aktivnosti. Kraće pauze ako osjećate nelagodu.", "caution"),
            new("Unhealthy for Sensitive Groups", "Ograničite intenzivne treninge. Preferirajte zatvorene prostore.", "warning"),
            new("Unhealthy", "Izbjegavajte outdoor treninge. Koristite teretane i zatvorene objekte.", "danger"),
            new("Very Unhealthy", "Sve aktivnosti samo u zatvorenim prostorima s filtracijom zraka.", "danger"),
            new("Hazardous", "Otkazujte sve outdoor aktivnosti. Ostanite u zatvorenom.", "emergency")
        };

    private static List<CategoryRecommendation> GetChildrenRecommendations() =>
        new()
        {
            new("Good", "Djeca mogu nesmetano igrati vani. Poticajte outdoor aktivnosti.", "safe"),
            new("Moderate", "Većina djece može igrati vani, ali pazite na one s respiratornim problemima.", "caution"),
            new("Unhealthy for Sensitive Groups", "Ograničite vrijeme vani za svu djecu. Kratke šetnje su OK.", "warning"),
            new("Unhealthy", "Djeca treba da ostanu u zatvorenim prostorima. Izbjegavajte outdoor aktivnosti.", "danger"),
            new("Very Unhealthy", "Sve djeca unutra. Zatvorite prozore, koristite prečišćivače zraka.", "danger"),
            new("Hazardous", "Hitno: sva djeca ostaju u zatvorenim prostorima. Nositi maske ako je potrebno izaći.", "emergency")
        };

    private static List<CategoryRecommendation> GetElderlyRecommendations() =>
        new()
        {
            new("Good", "Sigurno za sve aktivnosti vani. Dobro vrijeme za šetnje i vrt.", "safe"),
            new("Moderate", "Ograničite naporne aktivnosti vani. Kratke šetnje su u redu.", "caution"),
            new("Unhealthy for Sensitive Groups", "Ostanite unutra ako imate bolesti srca ili pluća.", "warning"),
            new("Unhealthy", "Svi stariji ostaju u zatvorenom. Izbjegavajte sve outdoor aktivnosti.", "danger"),
            new("Very Unhealthy", "Ostanite unutra. Zatvorite prozore. Kontaktirajte ljekara pri problemima.", "danger"),
            new("Hazardous", "Hitno: ostanite u zatvorenom. Pozovite ljekara ako osjećate simptome.", "emergency")
        };

    private static List<CategoryRecommendation> GetAsthmaRecommendations() =>
        new()
        {
            new("Good", "Sigurno za sve aktivnosti. Redovito uzimajte lijekove.", "safe"),
            new("Moderate", "Oprez pri fizičkim aktivnostima. Imajte inhalator pri ruci.", "caution"),
            new("Unhealthy for Sensitive Groups", "Ograničite aktivnosti vani. Povećajte dozu lijekova ako je preporučeno.", "warning"),
            new("Unhealthy", "Ostanite u zatvorenom. Koristite inhalator preporučeno. Kontakt s ljekarom.", "danger"),
            new("Very Unhealthy", "Ostanite unutra. Pripremite rescue medikacije. Pozovite ljekara.", "danger"),
            new("Hazardous", "Hitno ostanite unutra. Imajte emergency lijekove. Pozovite hitnu ako je potrebno.", "emergency")
        };
}