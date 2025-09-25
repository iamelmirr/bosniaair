using Microsoft.AspNetCore.Mvc;
using SarajevoAir.Api.Services;

namespace SarajevoAir.Api.Controllers;

/*
===========================================================================================
                              HEALTH GROUPS ADVISORY CONTROLLER
===========================================================================================

PURPOSE & PERSONALIZED HEALTH GUIDANCE:
Specialized API endpoint za health-sensitive population advisory system.
Provides personalized air quality recommendations based on individual health conditions.

HEALTH-DRIVEN API ARCHITECTURE:
Unlike general AQI endpoints, this controller focuses specifically on health outcomes
Segments users into risk categories za targeted advisory messages
Bridges medical guidelines sa environmental data za actionable health guidance

POPULATION SEGMENTATION STRATEGY:
┌─────────────────────┐    ┌──────────────────────────┐    ┌─────────────────────┐
│   USER POPULATIONS  │────│   GROUPS CONTROLLER      │────│  HEALTH ADVISORY    │
│                     │    │   (Risk Assessment)      │    │     SERVICE         │
│ • General Public    │    │                          │    │                     │
│ • Respiratory       │    │                          │    │ • Risk Calculation  │
│ • Cardiovascular    │    │                          │    │ • Message Selection │
│ • Outdoor Workers   │    │                          │    │ • Threshold Logic   │
│ • Athletes          │    │                          │    │                     │
│ • Children/Elderly  │    │                          │    │                     │
└─────────────────────┘    └──────────────────────────┘    └─────────────────────┘

MEDICAL INTEGRATION VALUE:
- Enables healthcare providers to give air quality guidance
- Supports chronic disease management (asthma, COPD, heart disease)
- Facilitates public health messaging za vulnerable populations
- Provides evidence-based activity recommendations

PERSONALIZATION APPROACH:
Different AQI thresholds trigger warnings za different health groups
Graduated recommendations from "safe" to "avoid outdoor activities"
Evidence-based guidance aligned sa medical research i EPA guidelines
*/

/// <summary>
/// Health groups advisory API controller za personalized air quality guidance
/// Provides risk-stratified recommendations za health-sensitive populations
/// </summary>
[ApiController]
[Route("api/v1/groups")]
public class GroupsController : ControllerBase
{
    /*
    === HEALTH-FOCUSED SERVICE DEPENDENCIES ===
    
    MEDICAL ADVISORY INTEGRATION:
    IHealthAdviceService - Specialized service za health-based AQI analysis
    Transforms technical AQI data into actionable health recommendations
    */
    private readonly IHealthAdviceService _healthAdviceService;
    private readonly ILogger<GroupsController> _logger;

    /// <summary>
    /// Constructor za health advisory service integration
    /// </summary>
    public GroupsController(IHealthAdviceService healthAdviceService, ILogger<GroupsController> logger)
    {
        _healthAdviceService = healthAdviceService;
        _logger = logger;
    }

    /*
    === PERSONALIZED HEALTH ADVISORY ENDPOINT ===
    
    MULTI-GROUP RISK ASSESSMENT:
    Analyzes current AQI conditions against health group vulnerability profiles
    Returns personalized recommendations za each health-sensitive population
    
    MEDICAL DECISION SUPPORT:
    Enables healthcare applications to provide evidence-based air quality guidance
    Supports chronic disease management i preventive health strategies
    */
    
    /// <summary>
    /// Retrieves personalized health recommendations za all user groups
    /// Based on current air quality conditions i health vulnerability profiles
    /// 
    /// Example requests:
    /// GET /api/v1/groups - Sarajevo health group analysis (default)
    /// GET /api/v1/groups?city=Tuzla - Health recommendations za Tuzla
    /// 
    /// Response includes:
    /// - Risk assessment za each health-sensitive group
    /// - Personalized activity recommendations  
    /// - AQI threshold warnings za vulnerable populations
    /// - Evidence-based health guidance messages
    /// 
    /// Health groups analyzed:
    /// - General Population (standard EPA guidance)
    /// - Respiratory Sensitive (asthma, COPD, lung conditions)
    /// - Cardiovascular Sensitive (heart disease, hypertension)  
    /// - Outdoor Workers (construction, agriculture, sports)
    /// - Athletes (endurance training, competitive sports)
    /// - Children & Elderly (age-based vulnerability)
    /// </summary>
    /// <param name="city">City name za health advisory analysis</param>
    /// <param name="cancellationToken">Request cancellation support</param>
    /// <returns>Comprehensive health group recommendations sa risk assessments</returns>
    [HttpGet]
    public async Task<IActionResult> GetGroupRecommendations(
        [FromQuery] string city = "Sarajevo",
        CancellationToken cancellationToken = default)
    {
        try
        {
            /*
            === HEALTH ADVISORY PROCESSING ===
            
            MULTI-STEP ANALYSIS:
            1. Fetch current AQI data za specified city
            2. Assess risk levels za each health-sensitive group
            3. Generate personalized recommendations based on vulnerability profiles
            4. Format response sa actionable health guidance
            */
            var response = await _healthAdviceService.BuildGroupsResponseAsync(city, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            /*
            === HEALTH SERVICE ERROR HANDLING ===
            
            MEDICAL ADVISORY FAILURE SCENARIOS:
            AQI data unavailable za health assessment
            Service configuration issues
            External API failures affecting health calculations
            
            SAFE DEGRADATION:
            Health advisory failures should not expose sensitive information
            Generic error message protects system details
            */
            _logger.LogError(ex, "Failed to get group recommendations for city {City}", city);
            return StatusCode(500, new
            {
                message = "Failed to retrieve group recommendations"
            });
        }
    }
}