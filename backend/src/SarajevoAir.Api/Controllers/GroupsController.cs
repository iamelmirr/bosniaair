using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly IHealthAdviceService _healthAdviceService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IHealthAdviceService healthAdviceService, ILogger<GroupsController> logger)
    {
        _healthAdviceService = healthAdviceService;
        _logger = logger;
    }

    /// <summary>
    /// Get health recommendations for different user groups based on current AQI.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetGroupRecommendations(
        [FromQuery] string city = "Sarajevo",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _healthAdviceService.BuildGroupsResponseAsync(city, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get group recommendations for city {City}", city);
            return StatusCode(500, new
            {
                message = "Failed to retrieve group recommendations"
            });
        }
    }
}