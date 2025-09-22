using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Application.Dtos;
using SarajevoAir.Application.Interfaces;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ShareController : ControllerBase
{
    private readonly IShareService _shareService;
    private readonly ILogger<ShareController> _logger;

    public ShareController(IShareService shareService, ILogger<ShareController> logger)
    {
        _shareService = shareService;
        _logger = logger;
    }

    /// <summary>
    /// Generate shareable content for social media
    /// </summary>
    /// <param name="request">Share request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Formatted share content</returns>
    [HttpPost]
    public async Task<IActionResult> GenerateShareContent(
        [FromBody] ShareRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var shareContent = await _shareService.GenerateShareContentAsync(request, cancellationToken);
            
            _logger.LogInformation("Generated share content for {City} with AQI {Aqi}", 
                request.City, request.Aqi);
            
            return Ok(shareContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate share content");
            return StatusCode(500, new { 
                message = "Failed to generate share content"
            });
        }
    }

    /// <summary>
    /// Generate share content via GET (for simple links)
    /// </summary>
    /// <param name="city">City name</param>
    /// <param name="aqi">AQI value</param>
    /// <param name="category">AQI category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Formatted share content</returns>
    [HttpGet]
    public async Task<IActionResult> GenerateShareContentGet(
        [FromQuery] string? city = null,
        [FromQuery] int? aqi = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ShareRequestDto(city, aqi, category);
            var shareContent = await _shareService.GenerateShareContentAsync(request, cancellationToken);
            
            _logger.LogInformation("Generated share content via GET for {City} with AQI {Aqi}", city, aqi);
            
            return Ok(shareContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate share content via GET");
            return StatusCode(500, new { 
                message = "Failed to generate share content"
            });
        }
    }
}