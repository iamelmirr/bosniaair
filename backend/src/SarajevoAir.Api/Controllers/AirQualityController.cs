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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _airQualityService.GetLiveAsync(city, cancellationToken);
            return Ok(response);
        }
        catch (DataUnavailableException ex)
        {
            _logger.LogWarning(ex, "Live data unavailable for {City}", city);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Data unavailable", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve live air quality data for {City}", city);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error", message = ex.Message });
        }
    }

    [HttpGet("{city}/forecast")]
    [ProducesResponseType(typeof(ForecastResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ForecastResponse>> GetForecastAsync(
        [FromRoute] City city,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _airQualityService.GetForecastAsync(city, cancellationToken);
            return Ok(response);
        }
        catch (DataUnavailableException ex)
        {
            _logger.LogWarning(ex, "Forecast data unavailable for {City}", city);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Data unavailable", message = ex.Message });
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _airQualityService.GetCompleteAsync(city, cancellationToken);
            return Ok(response);
        }
        catch (DataUnavailableException ex)
        {
            _logger.LogWarning(ex, "Complete data unavailable for {City}", city);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Data unavailable", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve complete air quality data for {City}", city);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error", message = ex.Message });
        }
    }

}
