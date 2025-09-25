/*
=== HEALTHADVICESERVICE.CS ===
HEALTH-SENSITIVE GROUPS ADVISORY SERVICE

ARCHITECTURAL ROLE:
- Personalized health guidance za different population groups  
- Risk assessment based na current air quality conditions
- Actionable recommendations za vulnerable populations
- Public health awareness i safety support

BUSINESS VALUE:
1. Personalized advice za health-sensitive groups
2. Preventive health guidance (avoid hospital visits)
3. Outdoor activity planning za different populations
4. Public health policy i communication support

TARGET GROUPS:
1. Sportisti - Outdoor exercise i athletic activities
2. Djeca - Children's respiratory health protection
3. Stariji - Elderly population vulnerability  
4. Astmatičari - Asthma i respiratory condition management

DESIGN PATTERNS:
1. Health Group Strategy Pattern - Different advice za each group
2. Threshold-Based Assessment - AQI-driven recommendations  
3. Advisory Composition Pattern - Rich guidance information
4. Risk Communication Pattern - Clear, actionable messaging
*/

using System.Linq;
using Microsoft.Extensions.Logging;
using SarajevoAir.Api.Dtos;

namespace SarajevoAir.Api.Services;

/*
=== HEALTH ADVICE SERVICE INTERFACE ===

INTERFACE DESIGN:
- Single method za comprehensive health guidance generation
- City-based air quality assessment
- Rich response model sa all group recommendations
- Standard async patterns za consistency

HEALTH GUIDANCE SCOPE:
Multi-group assessment sa personalized recommendations
Risk-based advice za different vulnerability levels
*/

/// <summary>
/// Service interface za generating health-sensitive group guidance
/// Provides personalized recommendations based na air quality conditions
/// </summary>
public interface IHealthAdviceService
{
    /// <summary>
    /// Builds comprehensive health guidance za all sensitive groups
    /// </summary>
    /// <param name="city">Target city za air quality assessment</param>
    /// <param name="cancellationToken">Cancellation token support</param>
    /// <returns>Multi-group health advisory response</returns>
    Task<GroupsResponse> BuildGroupsResponseAsync(string city, CancellationToken cancellationToken = default);
}

/*
=== HEALTH ADVICE SERVICE IMPLEMENTATION ===

ADVISORY ORCHESTRATION:
Combines live air quality data sa health group expertise
Generates personalized recommendations za different vulnerabilities

SERVICE CHARACTERISTICS:
- Health-focused business logic (no database persistence)
- Risk assessment i communication specialization
- Multi-group advisory generation
- Clear, actionable guidance messaging
*/

/// <summary>
/// Implementation od health advisory logic za sensitive population groups
/// Combines air quality data sa health expertise za personalized guidance
/// </summary>
public class HealthAdviceService : IHealthAdviceService
{
    /*
    === SERVICE DEPENDENCIES ===
    
    HEALTH ADVISORY ARCHITECTURE:
    _airQualityService: Current air quality data source
    _logger: Health advisory operation tracking
    
    NO DATABASE DEPENDENCY:
    Health advice je rule-based (no historical data needed)
    Real-time assessment based na current conditions only
    */
    private readonly IAirQualityService _airQualityService; // Live AQI data source
    private readonly ILogger<HealthAdviceService> _logger;  // Advisory operation logging

    /*
    === HEALTH GROUP CONFIGURATION ===
    
    VULNERABLE POPULATIONS:
    Sportisti - High physical activity increases pollutant intake
    Djeca - Developing respiratory systems more susceptible
    Stariji - Age-related respiratory i cardiovascular vulnerability
    Astmatičari - Pre-existing respiratory conditions amplify risk
    
    STATIC CONFIGURATION:
    Group names u Serbian za local relevance
    Order reflects typical population size (largest to smallest)
    Easily extensible za additional groups
    */
    private static readonly string[] GroupNames = ["Sportisti", "Djeca", "Stariji", "Astmatičari"];

    /*
    === CONSTRUCTOR SA HEALTH ADVISORY DEPENDENCIES ===
    
    FOCUSED DEPENDENCY SET:
    Only air quality service i logging needed
    Health logic je rule-based (no external health APIs)
    */
    
    /// <summary>
    /// Constructor prima health advisory dependencies
    /// </summary>
    public HealthAdviceService(IAirQualityService airQualityService, ILogger<HealthAdviceService> logger)
    {
        _airQualityService = airQualityService;
        _logger = logger;
    }

    /*
    === MAIN HEALTH ADVISORY GENERATION METHOD ===
    
    ADVISORY PIPELINE:
    1. Live air quality data retrieval
    2. Multi-group assessment generation
    3. Risk-based recommendation mapping
    4. Comprehensive response construction
    
    HEALTH ASSESSMENT STRATEGY:
    Single AQI value drives all group assessments
    Each group has different vulnerability thresholds
    Personalized recommendations based na group-specific risks
    
    REAL-TIME FOCUS:
    Health advice based na current conditions
    No historical analysis (immediate safety focus)
    */
    
    /// <summary>
    /// Generates comprehensive health advisory za all sensitive groups
    /// Based na current air quality conditions
    /// </summary>
    public async Task<GroupsResponse> BuildGroupsResponseAsync(string city, CancellationToken cancellationToken = default)
    {
        /*
        === LIVE AIR QUALITY RETRIEVAL ===
        
        CACHED DATA STRATEGY:
        forceFresh: false allows cache hits za performance
        Health advice ne requires absolute latest data
        10-minute cache staleness acceptable za advisory purposes
        
        AQI AS PRIMARY METRIC:
        OverallAqi drives all health assessments
        Single value simplifies cross-group comparison
        */
        var live = await _airQualityService.GetLiveAqiAsync(city, forceFresh: false, cancellationToken);

        /*
        === MULTI-GROUP ASSESSMENT GENERATION ===
        
        FUNCTIONAL PROCESSING:
        LINQ Select transforms each group name u complete advisory
        BuildGroupStatus encapsulates group-specific logic
        ToList materializes za immediate processing
        
        CONSISTENT ASSESSMENT:
        Same AQI value used za all groups
        Group-specific thresholds i recommendations
        Uniform assessment timestamp
        */
        var groupStatuses = GroupNames
            .Select(name => BuildGroupStatus(name, live.OverallAqi))
            .ToList();

        /*
        === ADVISORY OPERATION LOGGING ===
        
        HEALTH MONITORING:
        Tracks advisory generation za public health metrics
        AQI context enables health impact analysis
        Useful za understanding advisory request patterns
        */
        _logger.LogInformation("Generated health guidance for {City} at AQI {Aqi}", live.City, live.OverallAqi);

        /*
        === COMPREHENSIVE ADVISORY RESPONSE ===
        
        RICH HEALTH CONTEXT:
        - City: Location context za advisory
        - CurrentAqi: Numeric value za reference
        - AqiCategory: EPA classification za general understanding
        - Groups: Detailed group-specific advisories
        - Timestamp: Advisory generation time
        
        IMMUTABLE RESPONSE:
        Record type ensures thread-safe advisory data
        Complete information za health decision making
        */
        return new GroupsResponse(
            City: live.City,                    // Confirmed city name
            CurrentAqi: live.OverallAqi,        // Current AQI reading
            AqiCategory: live.AqiCategory,      // EPA classification
            Groups: groupStatuses,              // All group advisories
            Timestamp: DateTime.UtcNow          // Advisory generation timestamp
        );
    }

    /*
    === GROUP-SPECIFIC ADVISORY CONSTRUCTION ===
    
    PURPOSE:
    Creates comprehensive health advisory za single population group
    Combines group metadata sa current risk assessment
    
    ADVISORY COMPONENTS:
    1. Group definition (thresholds, descriptions, icons)
    2. Current recommendation based na AQI level
    3. Risk assessment za this group
    
    DESIGN PATTERN:
    Builder pattern - assemble complex advisory from components
    Separation između group metadata i current assessment
    */
    
    /// <summary>
    /// Builds complete advisory za specific health group
    /// Combines static group data sa current AQI assessment
    /// </summary>
    private static GroupStatusDto BuildGroupStatus(string groupName, int currentAqi)
    {
        /*
        === GROUP METADATA CONSTRUCTION ===
        
        STATIC GROUP DATA:
        HealthGroupDto contains all static group information:
        - Vulnerability thresholds
        - Complete recommendation set
        - UI elements (icons, descriptions)
        
        METADATA SOURCING:
        Multiple utility functions provide group-specific data
        Centralized group knowledge u dedicated methods
        */
        var group = new HealthGroupDto(
            GroupName: groupName,                          // Group identifier
            AqiThreshold: GetThreshold(groupName),         // Risk threshold AQI
            Recommendations: GetRecommendations(groupName), // All scenario recommendations
            IconEmoji: GetIcon(groupName),                 // UI emoji icon
            Description: GetDescription(groupName)         // Group description
        );

        /*
        === CURRENT ASSESSMENT GENERATION ===
        
        DYNAMIC EVALUATION:
        Current recommendation based na AQI level i group thresholds
        Risk level assessment specific za this group's vulnerability
        
        CONTEXT-AWARE SELECTION:
        Same AQI može mean different risks za different groups
        Group-specific thresholds drive personalized advice
        */
        var recommendation = GetCurrentRecommendation(group.Recommendations, currentAqi);
        var riskLevel = GetRiskLevel(currentAqi, groupName);

        /*
        === COMPLETE ADVISORY ASSEMBLY ===
        
        COMPREHENSIVE STATUS:
        GroupStatusDto combines static metadata sa dynamic assessment
        Complete advisory information za single group
        Ready za UI consumption
        */
        return new GroupStatusDto(group, recommendation, riskLevel);
    }

    /*
    === GROUP-SPECIFIC HEALTH RECOMMENDATIONS ===
    
    PURPOSE:
    Defines comprehensive advisory messages za each health group
    Maps AQI categories na actionable health guidance
    
    RECOMMENDATION STRUCTURE:
    Each group has 6 messages covering all EPA AQI categories:
    - Good (0-50): Encouraging outdoor activity
    - Moderate (51-100): Cautious outdoor activity
    - Unhealthy for Sensitive (101-150): Limited activity warning
    - Unhealthy (151-200): Indoor recommendation
    - Very Unhealthy (201-300): Strong indoor guidance
    - Hazardous (301+): Emergency indoor requirement
    
    LANGUAGE STRATEGY:
    Serbian language za local relevance
    Clear, actionable guidance (not technical jargon)
    Positive framing when possible
    Escalating urgency sa AQI severity
    */
    
    /// <summary>
    /// Returns complete recommendation set za specific health group
    /// Covers all EPA AQI categories sa actionable guidance
    /// </summary>
    private static GroupRecommendations GetRecommendations(string groupName) => groupName switch
    {
        /*
        === SPORTISTI (ATHLETES/ACTIVE PEOPLE) ===
        
        VULNERABILITY FACTOR:
        High physical activity increases breathing rate i pollutant intake
        Outdoor exercise amplifies exposure risk
        Performance i recovery affected by poor air quality
        
        RECOMMENDATION STRATEGY:
        Focus na activity modification i location choices
        Indoor alternatives emphasized for poor conditions
        Performance optimization messaging
        */
        "Sportisti" => new GroupRecommendations(
            Good: "Idealno vrijeme za sve sportske aktivnosti. Uživajte u treningu vani!",              // Encourage full outdoor activity
            Moderate: "Dobro za većinu aktivnosti. Kraće pauze ako osjećate nelagodu.",                // Monitor comfort levels  
            UnhealthyForSensitive: "Ograničite intenzivne treninge. Preferirajte zatvorene prostore.",  // Reduce intensity
            Unhealthy: "Izbjegavajte outdoor treninge. Koristite teretane i zatvorene objekte.",        // Move indoors
            VeryUnhealthy: "Sve aktivnosti samo u zatvorenim prostorima s filtracijom zraka.",          // Filtered air only
            Hazardous: "Otkazujte sve outdoor aktivnosti. Ostanite u zatvorenom."),                    // Cancel outdoor exercise
        
        /*
        === DJECA (CHILDREN) ===
        
        VULNERABILITY FACTORS:
        Developing respiratory systems more susceptible
        Higher breathing rates za body size
        Less awareness od symptoms i self-limitation
        Longer lifetime exposure implications
        
        RECOMMENDATION STRATEGY:
        Parent/caregiver guidance language
        Protective messaging sa alternatives
        Emergency language za severe conditions
        School i playground considerations
        */
        "Djeca" => new GroupRecommendations(
            Good: "Djeca mogu nesmetano igrati vani. Poticajte outdoor aktivnosti.",                                    // Encourage outdoor play
            Moderate: "Većina djece može igrati vani, ali pazite na one s respiratornim problemima.",                  // Monitor sensitive children
            UnhealthyForSensitive: "Ograničite vrijeme vani za svu djecu. Kratke šetnje su OK.",                      // Limit outdoor time
            Unhealthy: "Djeca treba da ostanu u zatvorenim prostorima. Izbjegavajte outdoor aktivnosti.",             // Keep indoors
            VeryUnhealthy: "Sve djeca unutra. Zatvorite prozore, koristite prečišćivače zraka.",                      // Sealed indoor environment
            Hazardous: "Hitno: sva djeca ostaju u zatvorenim prostorima. Nositi maske ako je potrebno izaći."),       // Emergency protocols
        
        /*
        === STARIJI (ELDERLY) ===
        
        VULNERABILITY FACTORS:
        Age-related cardiovascular i respiratory decline
        Multiple comorbidities common
        Medication interactions possible
        Reduced physiological reserves
        
        RECOMMENDATION STRATEGY:
        Medical consultation guidance
        Comfort i safety focus
        Clear emergency indicators
        Caregiver communication
        */
        "Stariji" => new GroupRecommendations(
            Good: "Sigurno za sve aktivnosti vani. Dobro vrijeme za šetnje i vrt.",                                    // Encourage gentle outdoor activity
            Moderate: "Ograničite naporne aktivnosti vani. Kratke šetlje su u redu.",                               // Limit exertion
            UnhealthyForSensitive: "Ostanite unutra ako imate bolesti srca ili pluća.",                              // Consider comorbidities  
            Unhealthy: "Svi stariji ostaju u zatvorenom. Izbjegavajte sve outdoor aktivnosti.",                      // Universal indoor guidance
            VeryUnhealthy: "Ostanite unutra. Zatvorite prozore. Kontaktirajte ljekara pri problemima.",              // Medical consultation readiness
            Hazardous: "Hitno: ostanite u zatvorenom. Pozovite ljekara ako osjećate simptome."),                    // Emergency medical guidance
        
        /*
        === ASTMATIČARI (PEOPLE WITH ASTHMA) ===
        
        VULNERABILITY FACTORS:
        Pre-existing respiratory hypersensitivity
        Trigger-based symptom escalation
        Medication dependency za management
        Emergency care potentially needed
        
        RECOMMENDATION STRATEGY:
        Medication compliance emphasis
        Emergency preparedness
        Healthcare provider communication
        Strict exposure avoidance
        */
        "Astmatičari" => new GroupRecommendations(
            Good: "Sigurno za sve aktivnosti. Redovito uzimajte lijekove.",
            Moderate: "Oprez pri fizičkim aktivnostima. Imajte inhalator pri ruci.",
            UnhealthyForSensitive: "Ograničite aktivnosti vani. Povećajte dozu lijekova ako je preporučeno.",
            Unhealthy: "Ostanite u zatvorenom. Koristite inhalator preporučeno. Kontakt s ljekarom.",
            VeryUnhealthy: "Ostanite unutra. Pripremite rescue medikacije. Pozovite ljekara.",
            Hazardous: "Hitno ostanite unutra. Imajte emergency lijekove. Pozovite hitnu ako je potrebno."),
        _ => new GroupRecommendations("", "", "", "", "", "")
    };

    private static string GetCurrentRecommendation(GroupRecommendations recommendations, int aqi)
    {
        var category = GetAqiCategory(aqi);
        return category switch
        {
            "Good" => recommendations.Good,
            "Moderate" => recommendations.Moderate,
            "Unhealthy for Sensitive Groups" => recommendations.UnhealthyForSensitive,
            "Unhealthy" => recommendations.Unhealthy,
            "Very Unhealthy" => recommendations.VeryUnhealthy,
            "Hazardous" => recommendations.Hazardous,
            _ => "Provjerite kvalitet zraka prije izlaska."
        };
    }

    private static string GetRiskLevel(int aqi, string groupName)
    {
        var threshold = GetThreshold(groupName);
        return aqi switch
        {
            <= 50 => "low",
            <= 100 when aqi <= threshold => "low",
            <= 100 => "moderate",
            <= 150 => "moderate",
            <= 200 => "high",
            _ => "very-high"
        };
    }

    private static int GetThreshold(string groupName) => groupName switch
    {
        "Sportisti" => 100,
        "Djeca" => 75,
        "Stariji" => 75,
        "Astmatičari" => 50,
        _ => 100
    };

    private static string GetIcon(string groupName) => groupName switch
    {
        "Sportisti" => "🏃‍♂️",
        "Djeca" => "👶",
        "Stariji" => "👴",
        "Astmatičari" => "🫁",
        _ => "👤"
    };

    private static string GetDescription(string groupName) => groupName switch
    {
        "Sportisti" => "Preporuke za sportske aktivnosti i vežbanje na osnovu kvaliteta zraka",
        "Djeca" => "Posebne preporuke za zaštitu djece od zagađenja zraka",
        "Stariji" => "Savjeti za starije osobe (65+) i one s kroničnim bolestima",
        "Astmatičari" => "Specijalni savjeti za astmatičare i osobe s respiratornim problemima",
        _ => "Zdravstvene preporuke na osnovu kvaliteta zraka"
    };

    private static string GetAqiCategory(int aqi) => aqi switch
    {
        <= 50 => "Good",
        <= 100 => "Moderate", 
        <= 150 => "Unhealthy for Sensitive Groups",
        <= 200 => "Unhealthy",
        <= 300 => "Very Unhealthy",
        _ => "Hazardous"
    };
}

/*
=== HEALTHADVICESERVICE CLASS SUMMARY ===

ARCHITECTURAL OVERVIEW:
Health-focused advisory service providing personalized guidance za vulnerable populations
Rule-based system combining air quality data sa health expertise

KEY DESIGN PATTERNS:
1. Health Group Strategy - Different advice za each vulnerable population
2. Threshold-Based Assessment - AQI-driven risk evaluation
3. Recommendation Mapping - EPA categories → actionable guidance
4. Multi-Language Support - Local Serbian language za accessibility
5. Risk Communication - Clear, escalating urgency messaging

HEALTH GROUPS SUPPORTED:
- Sportisti: Athletic/active populations sa increased exposure risk
- Djeca: Children sa developing respiratory systems
- Stariji: Elderly sa age-related vulnerabilities  
- Astmatičari: Asthma patients sa pre-existing conditions

RECOMMENDATION STRUCTURE:
Each group has comprehensive guidance za all EPA AQI levels:
- Encouraging messages za good conditions
- Cautionary advice za moderate conditions
- Strong warnings za unhealthy conditions
- Emergency protocols za hazardous conditions

BUSINESS VALUE:
- Personalized health protection za vulnerable populations
- Preventive guidance reducing healthcare burden
- Public health communication i awareness
- Activity planning support za different risk levels

INTEGRATION POINTS:
- GroupsController: Health advisory HTTP endpoints
- AirQualityService: Current AQI data source
- Frontend health components: Rich advisory visualization
- Public health communication systems

LOCALIZATION FEATURES:
- Serbian language za local relevance
- Cultural context u health messaging
- Regional health authority alignment
- Community-appropriate guidance levels
*/
