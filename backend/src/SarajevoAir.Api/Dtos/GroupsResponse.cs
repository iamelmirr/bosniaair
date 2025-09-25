namespace SarajevoAir.Api.Dtos;

public record HealthGroupDto(
    string GroupName,
    int AqiThreshold,
    GroupRecommendations Recommendations,
    string IconEmoji,
    string Description
);

public record GroupRecommendations(
    string Good,
    string Moderate,
    string UnhealthyForSensitive,
    string Unhealthy,
    string VeryUnhealthy,
    string Hazardous
);

public record GroupStatusDto(
    HealthGroupDto Group,
    string CurrentRecommendation,
    string RiskLevel
);

public record GroupsResponse(
    string City,
    int CurrentAqi,
    string AqiCategory,
    IReadOnlyList<GroupStatusDto> Groups,
    DateTime Timestamp
);
