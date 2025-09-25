using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc;

using SarajevoAir.Api.Services;using SarajevoAir.Api.Services;



namespace SarajevoAir.Api.Controllers;namespace SarajevoAir.Api.Controllers;



[ApiController][ApiController]

[Route("api/v1/[controller]")][Route("api/v1/[controller]")]

public class AdminController : ControllerBasepublic class AdminController : ControllerBase

{{

    private readonly IAqiAdminService _aqiAdminService;    private readonly IAqiAdminService _aqiAdminService;

    private readonly ILogger<AdminController> _logger;    private readonly ILogger<AdminController> _logger;



    public AdminController(IAqiAdminService aqiAdminService, ILogger<AdminController> logger)    public AdminController(

    {        IAqiAdminService aqiAdminService,

        _aqiAdminService = aqiAdminService;        ILogger<AdminController> logger)

        _logger = logger;    {

    }        _aqiAdminService = aqiAdminService;

        _logger = logger;

    /// <summary>    }

    /// Get all stored Sarajevo AQI snapshots.

    /// </summary>    /// <summary>

    [HttpGet("aqi-records")]    /// Get all stored Sarajevo AQI snapshots.

    public async Task<IActionResult> GetAqiRecords(CancellationToken cancellationToken)    /// </summary>

    {    [HttpGet("aqi-records")]

        try    public async Task<IActionResult> GetAqiRecords(CancellationToken cancellationToken)

        {    {

            var result = await _aqiAdminService.GetAllRecordsAsync(cancellationToken);        try

            return Ok(result);        {

        }            var result = await _aqiAdminService.GetAllRecordsAsync(cancellationToken);

        catch (Exception ex)            return Ok(result);

        {        }

            _logger.LogError(ex, "Failed to get AQI records");        catch (Exception ex)

            return StatusCode(500, new { error = "Failed to fetch AQI records", details = ex.Message });        {

        }            _logger.LogError(ex, "Failed to get AQI records");

    }            return StatusCode(500, new { error = "Failed to fetch AQI records", details = ex.Message });

        }

    /// <summary>    }

    /// Delete an AQI record by ID.

    /// </summary>    /// <summary>

    [HttpDelete("aqi-records/{id:int}")]    /// Delete an AQI record by ID.

    public async Task<IActionResult> DeleteAqiRecord(int id, CancellationToken cancellationToken)    /// </summary>

    {    [HttpDelete("aqi-records/{id:int}")]

        try    public async Task<IActionResult> DeleteAqiRecord(int id, CancellationToken cancellationToken)

        {    {

            await _aqiAdminService.DeleteRecordAsync(id, cancellationToken);        try

            return Ok(new { message = $"AQI record {id} deleted successfully", deletedId = id });        {

        }            await _aqiAdminService.DeleteRecordAsync(id, cancellationToken);

        catch (Exception ex)            return Ok(new { message = $"AQI record {id} deleted successfully", deletedId = id });

        {        }

            _logger.LogError(ex, "Failed to delete AQI record {Id}", id);        catch (Exception ex)

            return StatusCode(500, new { error = "Failed to delete AQI record", details = ex.Message });        {

        }            _logger.LogError(ex, "Failed to delete AQI record {Id}", id);

    }            return StatusCode(500, new { error = "Failed to delete AQI record", details = ex.Message });

}        }

    }
}