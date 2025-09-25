using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly IAqiAdminService _aqiAdminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAqiAdminService aqiAdminService, ILogger<AdminController> logger)
    {
        _aqiAdminService = aqiAdminService;
        _logger = logger;
    }

    [HttpGet("aqi-records")]
    public async Task<IActionResult> GetAqiRecords(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _aqiAdminService.GetAllRecordsAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AQI records");
            return StatusCode(500, new { error = "Failed to fetch AQI records", details = ex.Message });
        }
    }

    [HttpDelete("aqi-records/{id:int}")]
    public async Task<IActionResult> DeleteAqiRecord(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _aqiAdminService.DeleteRecordAsync(id, cancellationToken);
            return Ok(new { message = $"AQI record {id} deleted successfully", deletedId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete AQI record {Id}", id);
            return StatusCode(500, new { error = "Failed to delete AQI record", details = ex.Message });
        }
    }
}