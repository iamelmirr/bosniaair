/*
=== WAQI (World Air Quality Index) API CONFIGURATION ===

HIGH LEVEL OVERVIEW:
Ova klasa definiše konfiguraciju za spajanje na WAQI API (api.waqi.info)
Koristi se kroz Options Pattern u ASP.NET Core

DESIGN PATTERNS:
1. OPTIONS PATTERN - type-safe binding iz appsettings.json
2. FALLBACK VALUES - default prazan string umesto null reference exceptions

INTEGRATION FLOW:
appsettings.json → IConfiguration → AqicnConfiguration → AqicnClient → HTTP API

LOW LEVEL DETAILS:
- Properties se bind-uju automatski iz "Aqicn" sekcije u appsettings.json
*/

namespace SarajevoAir.Api.Configuration;

/*
=== WAQI API CONFIGURATION CLASS ===
Ova klasa se koristi za type-safe pristup WAQI API konfiguraciji
Registruje se u Program.cs sa: Configure<AqicnConfiguration>(config.GetSection("Aqicn"))
*/
public class AqicnConfiguration
{
    /*
    === API CONNECTION PROPERTIES ===
    Ove properties se automatski populate-uju iz appsettings.json:
    
    appsettings.json:
    {
      "Aqicn": {
        "ApiUrl": "https://api.waqi.info",
        "ApiToken": "your-api-token-here"
      }
    }
    */
    
    /// <summary>
    /// Base URL za WAQI API - obično "https://api.waqi.info"
    /// Koristi se kao prefix za sve API pozive
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// API token za autentifikaciju sa WAQI servisom
    /// Dobija se registracijom na https://aqicn.org/data-platform/token/
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;
}