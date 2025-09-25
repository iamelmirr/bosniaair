namespace SarajevoAir.Api.Dtos;

/*
===========================================================================================
                              HEALTH GROUPS DATA TRANSFER OBJECTS
===========================================================================================

PURPOSE & HEALTH-FOCUSED API:
DTOs za health-sensitive population groups i personalized air quality recommendations.
Enables targeted health advisory based on individual sensitivity levels.

HEALTH GROUP CATEGORIZATION:
- General Population: Standard EPA recommendations
- Sensitive Groups: Children, elderly, pregnant women
- Respiratory Conditions: Asthma, COPD, lung disease patients  
- Cardiovascular Conditions: Heart disease, hypertension patients
- Outdoor Workers: Construction, agriculture, sports professionals
- Athletes: Endurance training, competitive sports

PERSONALIZED HEALTH ADVISORY:
Different AQI thresholds trigger warnings za different groups
Granular recommendations based on specific health vulnerabilities
Risk-stratified messaging za informed decision making
*/

/*
=== HEALTH GROUP DEFINITION ===

POPULATION SEGMENT PROFILE:
Defines characteristics i thresholds za specific health-sensitive group
Contains complete advisory framework za that population segment
*/

/// <summary>
/// Health-sensitive population group definition sa advisory framework
/// Contains group characteristics i AQI-specific recommendations
/// </summary>
/// <param name="GroupName">Human-readable group identifier (e.g., "Respiratory Sensitive")</param>
/// <param name="AqiThreshold">AQI level where special precautions begin za this group</param>
/// <param name="Recommendations">Complete set od AQI-level specific recommendations</param>
/// <param name="IconEmoji">Visual emoji representation za UI display</param>
/// <param name="Description">Detailed explanation od who belongs to this group</param>
public record HealthGroupDto(
    string GroupName,
    int AqiThreshold,
    GroupRecommendations Recommendations,
    string IconEmoji,
    string Description
);

/*
=== AQI-LEVEL SPECIFIC RECOMMENDATIONS ===

GRADUATED ADVISORY SYSTEM:
Different recommendation messages za each EPA AQI category
Enables nuanced health guidance based on specific air quality conditions
*/

/// <summary>
/// Complete set od health recommendations za all AQI categories
/// Provides graduated advisory from good to hazardous conditions
/// </summary>
/// <param name="Good">Recommendation when AQI 0-50 (minimal risk)</param>
/// <param name="Moderate">Recommendation when AQI 51-100 (acceptable za most)</param>
/// <param name="UnhealthyForSensitive">Recommendation when AQI 101-150 (sensitive group risk)</param>
/// <param name="Unhealthy">Recommendation when AQI 151-200 (everyone at risk)</param>
/// <param name="VeryUnhealthy">Recommendation when AQI 201-300 (health warnings)</param>
/// <param name="Hazardous">Recommendation when AQI 301+ (emergency conditions)</param>
public record GroupRecommendations(
    string Good,
    string Moderate,
    string UnhealthyForSensitive,
    string Unhealthy,
    string VeryUnhealthy,
    string Hazardous
);

/*
=== CURRENT GROUP STATUS ===

REAL-TIME HEALTH ADVISORY:
Combines group definition sa current AQI conditions
Provides immediate actionable recommendations za specific group
*/

/// <summary>
/// Current status i recommendations za specific health group
/// Combines group definition sa real-time air quality assessment
/// </summary>
/// <param name="Group">Health group definition i characteristics</param>
/// <param name="CurrentRecommendation">Active recommendation based on current AQI</param>
/// <param name="RiskLevel">Current risk assessment za this group (Low/Moderate/High/Extreme)</param>
public record GroupStatusDto(
    HealthGroupDto Group,
    string CurrentRecommendation,
    string RiskLevel
);

/*
=== COMPLETE HEALTH GROUPS RESPONSE ===

COMPREHENSIVE HEALTH ADVISORY:
Aggregates all health groups sa current conditions
Enables personalized air quality guidance za all user types
*/

/// <summary>
/// Complete health groups analysis za current air quality conditions
/// Provides personalized recommendations za all health-sensitive populations
/// 
/// JSON Response Example:
/// {
///   "city": "Sarajevo",
///   "currentAqi": 87,
///   "aqiCategory": "Moderate",
///   "groups": [
///     {
///       "group": {
///         "groupName": "Respiratory Sensitive",
///         "aqiThreshold": 100,
///         "iconEmoji": "🫁",
///         "description": "People with asthma, COPD, or other lung conditions"
///       },
///       "currentRecommendation": "Consider reducing prolonged outdoor activities",
///       "riskLevel": "Moderate"
///     }
///   ],
///   "timestamp": "2024-03-15T14:30:00Z"
/// }
/// </summary>
/// <param name="City">Target city za health advisory</param>
/// <param name="CurrentAqi">Current AQI value driving recommendations</param>
/// <param name="AqiCategory">Current EPA AQI category</param>
/// <param name="Groups">Array od health group statuses sa personalized recommendations</param>
/// <param name="Timestamp">UTC timestamp od analysis</param>
public record GroupsResponse(
    string City,
    int CurrentAqi,
    string AqiCategory,
    IReadOnlyList<GroupStatusDto> Groups,
    DateTime Timestamp
);
