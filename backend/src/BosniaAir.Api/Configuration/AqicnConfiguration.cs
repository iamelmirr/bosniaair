namespace BosniaAir.Api.Configuration;

/// <summary>
/// Configuration settings for the WAQI (World Air Quality Index) API.
/// Contains API endpoint URL and authentication token.
/// </summary>
public class AqicnConfiguration
{
    /// <summary>
    /// Base URL for the WAQI API endpoints
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// API token for authenticating requests to WAQI
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;
}