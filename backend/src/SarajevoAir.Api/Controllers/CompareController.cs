using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/compare")]
public class CompareController : ControllerBase
{
    private readonly ICityComparisonService _comparisonService;
    private readonly ILogger<CompareController> _logger;

    public CompareController(ICityComparisonService comparisonService, ILogger<CompareController> logger)
    {
        _comparisonService = comparisonService;
        _logger = logger;
    }

    /// <summary>
    /// Compare current AQI across a list of cities. Always performs fresh API calls.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CompareCities(
        [FromQuery] string cities = "Sarajevo",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _comparisonService.CompareCitiesAsync(cities, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare cities {Cities}", cities);
            return StatusCode(500, new
            {
                error = "Failed to compare cities",
                details = ex.Message
            });
        }
    }
}
