using Microsoft.AspNetCore.Mvc;
using BosniaAir.Api.Dtos;
using BosniaAir.Api.Enums;
using BosniaAir.Api.Services;

namespace BosniaAir.Api.Controllers;

/// <summary>
/// REST API controller for air quality data operations.
/// Provides endpoints for retrieving live, forecast, and complete air quality information.
/// </summary>
[ApiController]
[Route("api/v1/air-quality")]
[Produces("application/json")]
public class AirQualityController : ControllerBase
{
    private readonly IAirQualityService _airQualityService;
    private readonly ILogger<AirQualityController> _logger;

    /// <summary>
    /// Initializes a new instance of the AirQualityController.
    /// </summary>
    /// <param name="airQualityService">Service for air quality data operations</param>
    /// <param name="logger">Logger for recording operations and errors</param>
    public AirQualityController(IAirQualityService airQualityService, ILogger<AirQualityController> logger)
    {
        _airQualityService = airQualityService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves live air quality data for a specific city.
    /// </summary>
    /// <param name="city">The city identifier</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Live AQI data including current measurements and health advice</returns>
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

    /// <summary>
    /// Retrieves air quality forecast data for a specific city.
    /// </summary>
    /// <param name="city">The city identifier</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Forecast data with daily AQI predictions</returns>
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

    /// <summary>
    /// Retrieves complete air quality data including live and forecast information for a specific city.
    /// </summary>
    /// <param name="city">The city identifier</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Complete AQI data combining live measurements and forecast</returns>
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
