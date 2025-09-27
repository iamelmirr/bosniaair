using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Enums;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/air-quality")]
[Produces("application/json")]
public class AirQualityController : ControllerBase
{
    private readonly IAirQualityService _airQualityService;
    private readonly ILogger<AirQualityController> _logger;

    public AirQualityController(IAirQualityService airQualityService, ILogger<AirQualityController> logger)
    {
        _airQualityService = airQualityService;
        _logger = logger;
    }

    [HttpGet("{city}/live")]
    [ProducesResponseType(typeof(LiveAqiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LiveAqiResponse>> GetLiveAsync(
        [FromRoute] City city,
        [FromQuery] bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _airQualityService.GetLiveAsync(city, forceRefresh, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve live air quality data for {City}", city);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error", message = ex.Message });
        }
    }

    [HttpGet("{city}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<LiveAqiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<LiveAqiResponse>>> GetHistoryAsync(
        [FromRoute] City city,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return BadRequest(new { error = "Bad Request", message = "History limit must be greater than zero." });
        }

        try
        {
            var response = await _airQualityService.GetHistoryAsync(city, limit, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve history for {City}", city);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error", message = ex.Message });
        }
    }

    [HttpGet("{city}/forecast")]
    [ProducesResponseType(typeof(ForecastResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ForecastResponse>> GetForecastAsync(
        [FromRoute] City city,
        [FromQuery] bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _airQualityService.GetForecastAsync(city, forceRefresh, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve forecast data for {City}", city);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error", message = ex.Message });
        }
    }

    [HttpGet("{city}/complete")]
    [ProducesResponseType(typeof(CompleteAqiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CompleteAqiResponse>> GetCompleteAsync(
        [FromRoute] City city,
        [FromQuery] bool forceLiveRefresh = false,
        [FromQuery] bool forceForecastRefresh = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _airQualityService.GetCompleteAsync(city, forceLiveRefresh, forceForecastRefresh, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve complete air quality data for {City}", city);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error", message = ex.Message });
        }
    }

    [HttpGet("snapshots")]
    [ProducesResponseType(typeof(IDictionary<City, LiveAqiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyDictionary<City, LiveAqiResponse>>> GetSnapshotsAsync(
        [FromQuery] City[]? cities,
        CancellationToken cancellationToken = default)
    {
        var targetCities = cities is { Length: > 0 } ? cities : Enum.GetValues<City>();

        try
        {
            var response = await _airQualityService.GetLatestSnapshotsAsync(targetCities, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve snapshot summary");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error", message = ex.Message });
        }
    }

    [HttpPost("refresh/{city}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshCityAsync(
        [FromRoute] City city,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _airQualityService.RefreshCityAsync(city, cancellationToken);
            return Accepted(new { message = $"Refresh scheduled for {city}." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh data for {City}", city);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error", message = ex.Message });
        }
    }
}
