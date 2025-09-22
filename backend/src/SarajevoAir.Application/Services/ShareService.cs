using Microsoft.Extensions.Logging;
using SarajevoAir.Application.Dtos;
using SarajevoAir.Application.Interfaces;
using SarajevoAir.Domain.Aqi;

namespace SarajevoAir.Application.Services;

public class ShareService : IShareService
{
    private readonly ILogger<ShareService> _logger;

    public ShareService(ILogger<ShareService> logger)
    {
        _logger = logger;
    }

    public Task<ShareResponseDto> GenerateShareContentAsync(ShareRequestDto request, CancellationToken cancellationToken = default)
    {
        var city = request.City ?? "Sarajevo";
        var aqi = request.Aqi ?? 0;
        var category = request.Category ?? "Unknown";

        var title = $"SarajevoAir - {city} AQI {aqi}";
        var text = GenerateShareText(city, aqi, category);
        var url = "https://sarajevoair.vercel.app"; // This would come from configuration in real app

        _logger.LogInformation("Generated share content for {City} with AQI {Aqi}", city, aqi);

        return Task.FromResult(new ShareResponseDto(title, text, url));
    }

    private static string GenerateShareText(string city, int aqi, string category)
    {
        if (!Enum.TryParse<AqiCategory>(category, true, out var aqiCategory))
        {
            aqiCategory = AqiCategory.Good; // fallback
        }

        var recommendation = aqiCategory.GetRecommendation("bs");
        
        return aqiCategory switch
        {
            AqiCategory.Good => $"{city}: AQI {aqi} — {recommendation}",
            AqiCategory.Moderate => $"{city}: AQI {aqi} — {recommendation}",
            AqiCategory.USG or AqiCategory.Unhealthy => $"Upozorenje: {city} AQI {aqi} — {recommendation}",
            AqiCategory.VeryUnhealthy or AqiCategory.Hazardous => $"HITNO: {city} AQI {aqi} — {recommendation}",
            _ => $"{city}: AQI {aqi} — {recommendation}"
        };
    }
}